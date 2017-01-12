using Storj.net.Model;
using Storj.net.Model.EventArgs;
using Storj.net.Model.Exception;
using Storj.net.Network;
using Storj.net.Network.Request;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storj.net.File
{
    class FileDownloader
    {
        const int DEFAULT_DOWNLOAD_THREADS = 6;
        const int POINTERS_PER_REQUEST = 100;
        const int MAX_DOWNLOAD_RETRIES = 5;

        /* HOW THIS WORKS:
         * 
         * 1) /buckets/{BucketId}/tokens --> get token for pull
         * 2) retrieve token from storj bridge
         * 3) retrieve file pointers from storj bridge (frame)
         * 4) download shard(s) from farmer(s) -> HTTP download (GET) from http://farmer.address/shards/{Hash}?token={Token}/ with content application/octet-stream && header x-storj-node-id: {NodeId}
         * 5) piece file together and decrypt it
         * 
         */

        public string BucketId { get; set; }
        public string FileId { get; set; }
        public string Filename { get; set; }
        public int ConcurrentDownloadThreads { get; set; }
        public Action<DownloadProgressEventArgs> ProgressEvent { get; set; }
        public string Token { get { return token.TokenKey; } }


        private ShardingUtil sharder;
        private StorjFile file;
        private Token token;
        private Frame frame;
        private Cipher cipher;
        private List<FilePointer> pointers;
        private Queue<FilePointer> ptrWorkingQueue = new Queue<FilePointer>();
        private string cryptFilename;
        private List<Thread> downloadWorkers = new List<Thread>();
        private long bytesToDownload = 0;
        private long bytesDownloaded = 0;

        public FileDownloader(string bucketId, string fileId, string filename, Action<DownloadProgressEventArgs> progressEvent, string cipher = null)
        {
            this.BucketId = bucketId;
            this.Filename = filename;
            this.FileId = fileId;
            this.cryptFilename = filename + ".crypt";
            
            this.ConcurrentDownloadThreads = DEFAULT_DOWNLOAD_THREADS;
            this.cipher = (cipher == null ? null : Cipher.FromString(cipher));
        }

        public void Start()
        {
            //get additional file info
            StorjRestResponse<StorjFile> fileResponse = StorjRestClient.Request<StorjFile>(new GetFileRequest(BucketId, FileId));
            if (fileResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new GetFileInfoException(), fileResponse.Response);
            file = fileResponse.ToObject();

            //retrieve token
            StorjRestResponse<Token> tokenResponse = StorjRestClient.Request<Token>(new CreateTokenRequest(BucketId, Model.Operation.PULL));
            if (tokenResponse.StatusCode != System.Net.HttpStatusCode.Created)
                StorjClient.ThrowStorjResponseError(new TokenCreationException(), tokenResponse.Response);
            token = tokenResponse.ToObject();

            pointers = GetAllFilePointers();

            bytesToDownload = (long)pointers.Count * (long)StorjClient.ShardSize;

            if (pointers.Count < ConcurrentDownloadThreads)
                ConcurrentDownloadThreads = pointers.Count;

            //initialize sharder to piece file together
            sharder = new ShardingUtil(cryptFilename);

            //convert list of pointers to working queue for DL worker
            foreach (FilePointer ptr in pointers)
                ptrWorkingQueue.Enqueue(ptr);

            //start workers
            for (int i = 0; i < ConcurrentDownloadThreads; i++)
            {
                Thread worker = new Thread(new ThreadStart(DownloadWorker));
                downloadWorkers.Add(worker);
                worker.Start();

                //give threads a short time for initialization
                Thread.Sleep(10);
            }

            //join workers to wait for them to complete uploading shards
            foreach (Thread worker in downloadWorkers)
            {
                worker.Join();
                if (worker.ThreadState == ThreadState.Aborted)
                {
                    if (System.IO.File.Exists(cryptFilename))
                        System.IO.File.Delete(cryptFilename);
                    throw new ShardDownloadException();
                }
            }

            //remove zeros at the end of the file
            //they always exist due to standard size of a shard (e.g. 1MB)
            //if file is not exactly length % 1MB == 0 -> rest of the shard filled up with zeros
            sharder.FinalizeFile();

            //get cipher from keyring
            if (cipher == null)
                cipher = KeyRingUtil.Get(file.Filename);

            if (cipher == null)
            {
                if (System.IO.File.Exists(cryptFilename))
                    System.IO.File.Delete(cryptFilename);
                throw new CipherNotFoundException();
            }

            //decrypt file
            CryptoUtil.Decrypt(cryptFilename, Filename, cipher);

            //cleanup afterwards
            System.IO.File.Delete(cryptFilename);
        }

        private void DownloadWorker()
        {
            int retries = 0;

            while (ptrWorkingQueue.Count > 0)
            {
                FilePointer ptr = ptrWorkingQueue.Dequeue();
                try
                {
                    ProcessPointer(ptr);
                    bytesDownloaded += StorjClient.ShardSize;
                    ProgressUpdate();
                }
                catch (Exception e)
                {
                    retries++;
                    if (retries >= MAX_DOWNLOAD_RETRIES)
                        Thread.CurrentThread.Abort();
                }
            }
        }

        private void ProcessPointer(FilePointer pointer)
        {
            string shardPath = Path.Combine(StorjClient.ShardDirectory, RandomStringUtil.GenerateRandomName() + ".shard");
            HttpShardTransferUtil.DownloadShard(pointer, shardPath);
            
            sharder.AppendShard(shardPath);
        }

        private List<FilePointer> GetAllFilePointers()
        {
            List<FilePointer> allPointers = new List<FilePointer>();
            int pointersReceived = 0;
            do
            {
                StorjRestResponse<List<FilePointer>> filePointerResponse = StorjRestClient.Request<List<FilePointer>>(new GetFilePointersRequest(BucketId, FileId, token.TokenKey, POINTERS_PER_REQUEST, pointersReceived));
                if (filePointerResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    StorjClient.ThrowStorjResponseError(new GetFilePointerException(), filePointerResponse.Response);

                List<FilePointer> pointers = filePointerResponse.ToObject();

                if (pointers.Count == 0)
                    break;

                pointersReceived += pointers.Count;

                allPointers.AddRange(pointers);
            } while (pointersReceived % POINTERS_PER_REQUEST == 0);

            //no file pointers found
            if (allPointers.Count == 0)
                throw new GetFilePointerException();

            return allPointers;
        }

        private void ProgressUpdate()
        {
            if (ProgressEvent != null)
                ProgressEvent.BeginInvoke(new DownloadProgressEventArgs(bytesDownloaded, bytesToDownload), null, null);
        }
    }
}

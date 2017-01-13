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
        const int DEFAULT_DOWNLOAD_THREADS = 10;
        const int POINTERS_PER_REQUEST = 10;
        const int MAX_DOWNLOAD_RETRIES = 5;
        const int MAX_FILEPOINTER_RETRIES = 5;
        const int POINTER_RETRY_DELAY_IN_S = 5;

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
        private int shardDownloadsStarted = 0;
        private int shardsToDownload = 0;
        private long bytesToDownload = 0;
        private long bytesDownloaded = 0;
        private bool downloadAborted = false;

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
            Log.Debug("Requesting additional file info for file {0} in bucket {1}", FileId, BucketId);
            StorjRestResponse<StorjFile> fileResponse = StorjRestClient.Request<StorjFile>(new GetFileRequest(BucketId, FileId));
            if (fileResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new GetFileInfoException(), fileResponse.Response);
            file = fileResponse.ToObject();

            shardsToDownload = (int)(file.Size / StorjClient.ShardSize);

            Log.Debug("Requesting all file pointers");
            pointers = GetAllFilePointers();

            bytesToDownload = pointers.Count * StorjClient.ShardSize;

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
                if (downloadAborted)
                    break;
                worker.Join();
            }

            if (downloadAborted)
            {
                Log.Debug("Download of file {0} failed", Filename);
                if (System.IO.File.Exists(cryptFilename))
                    System.IO.File.Delete(cryptFilename);

                sharder.CleanUp();

                throw new ShardDownloadException();
            }

            //remove zeros at the end of the file
            //they always exist due to standard size of a shard (e.g. 1MB)
            //if file is not exactly length % 1MB == 0 -> rest of the shard filled up with zeros
            Log.Debug("Finalizing file {0}", Filename);
            sharder.FinalizeFile();

            //get cipher from keyring
            if (cipher == null)
                cipher = KeyRingUtil.Get(file.Filename);

            if (cipher == null)
            {
                Log.Debug("Decryption of file {0} failed because cipher could not be loaded", Filename);
                if (System.IO.File.Exists(cryptFilename))
                    System.IO.File.Delete(cryptFilename);
                throw new CipherNotFoundException();
            }

            //decrypt file
            Log.Debug("Attempting decryption of file {0}", Filename);
            CryptoUtil.Decrypt(cryptFilename, Filename, cipher);

            //cleanup afterwards
            System.IO.File.Delete(cryptFilename);
        }

        private void DownloadWorker()
        {
            int retries = 0;

            while (ptrWorkingQueue.Count > 0)
            {
                int currentShard = shardDownloadsStarted;
                shardDownloadsStarted++;

                FilePointer ptr = ptrWorkingQueue.Dequeue();

                while (retries < MAX_DOWNLOAD_RETRIES)
                {
                    try
                    {
                        Log.Debug("Attempting retrieval of shard {0}, attempt {1}", currentShard, retries);
                        ProcessPointer(ptr);
                        Log.Debug("Retrieval of shard {0} successfully completed", currentShard);
                        bytesDownloaded += StorjClient.ShardSize;
                        ProgressUpdate();
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Retrieval of shard {0} failed, attempt {1}", currentShard, retries);
                        retries++;

                        if (retries < MAX_DOWNLOAD_RETRIES)
                        {
                            ptr = RefreshPointer(ptr);
                            continue;
                        }

                        Log.Debug("Retrieval of shard {0} ultimately failed, after {1} attempts", currentShard, retries - 1);
                        downloadAborted = true;
                        Thread.CurrentThread.Abort();
                    }
                }
            }
        }

        private void RefreshToken()
        {
            Log.Debug("Retrieving token for operation");
            StorjRestResponse<Token> tokenResponse = StorjRestClient.Request<Token>(new CreateTokenRequest(BucketId, Model.Operation.PULL));
            if (tokenResponse.StatusCode != System.Net.HttpStatusCode.Created)
                StorjClient.ThrowStorjResponseError(new TokenCreationException(), tokenResponse.Response);
            token = tokenResponse.ToObject();
        }

        private void ProcessPointer(FilePointer pointer)
        {
            string shardPath = Path.Combine(StorjClient.ShardDirectory, RandomStringUtil.GenerateRandomName() + ".shard");
            HttpShardTransferUtil.DownloadShard(pointer, shardPath);
            
            sharder.AppendShard(shardPath);
        }

        private FilePointer RefreshPointer(FilePointer fp)
        {
            int retries = 0;
            StorjRestResponse<List<FilePointer>> filePointerResponse = null;

            while (retries < MAX_FILEPOINTER_RETRIES)
            {
                RefreshToken();
                Log.Debug("Requesting refresh of file pointer {0}, attempt {1}", fp.Index, retries);

                filePointerResponse = StorjRestClient.Request<List<FilePointer>>(new GetFilePointersRequest(BucketId, FileId, token.TokenKey, 1, fp.Index));

                if (filePointerResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Log.Debug("Requesting {0} file pointers failed, attempt {1}. Trying again in {2} seconds", 1, retries, POINTER_RETRY_DELAY_IN_S);
                    retries++;

                    if (retries < MAX_FILEPOINTER_RETRIES)
                    {
                        Thread.Sleep(POINTER_RETRY_DELAY_IN_S * 1000);
                        continue;
                    }

                    Log.Debug("Requesting {0} file pointers failed ultimately, after {1} attempts", 1, retries);
                    return null;
                }
                else
                    break;
            }

            List<FilePointer> pointers = filePointerResponse.ToObject();

            if (pointers.Count == 0)
                return null;

            Log.Debug("Received {0} file pointers in this request", pointers.Count);

            return pointers.First();
        }

        private List<FilePointer> GetAllFilePointers()
        {
            List<FilePointer> allPointers = new List<FilePointer>();

            /*for (int i = 0; i < shardsToDownload; i++)
            {
                int retries = 0;
                StorjRestResponse<List<FilePointer>> filePointerResponse = null;

                while (retries < MAX_FILEPOINTER_RETRIES)
                {
                    RefreshToken();
                    Log.Debug("Requesting {0} file pointers, attempt {1}", POINTERS_PER_REQUEST, retries);

                    filePointerResponse = StorjRestClient.Request<List<FilePointer>>(new GetFilePointersRequest(BucketId, FileId, token.TokenKey, 1, i));

                    if (filePointerResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Log.Debug("Requesting {0} file pointers failed, attempt {1}. Trying again in {2} seconds", 1, retries, POINTER_RETRY_DELAY_IN_S);
                        retries++;

                        if (retries < MAX_FILEPOINTER_RETRIES)
                        {
                            Thread.Sleep(POINTER_RETRY_DELAY_IN_S * 1000);
                            continue;
                        }

                        Log.Debug("Requesting {0} file pointers failed ultimately, after {1} attempts", 1, retries);
                        StorjClient.ThrowStorjResponseError(new GetFilePointerException(), filePointerResponse.Response);
                    }
                    else
                        break;
                }

                List<FilePointer> pointers = filePointerResponse.ToObject();

                if (pointers.Count == 0)
                    break;

                Log.Debug("Received {0} file pointers in this request", pointers.Count);

                allPointers.AddRange(pointers);
            }

            if (allPointers.Count == 0)
                throw new GetFilePointerException();

            Log.Debug("{0} file pointer received in total", allPointers.Count());

            return allPointers;*/

            int pointersReceived = 0;
            
            do
            {
                int retries = 0;
                StorjRestResponse<List<FilePointer>> filePointerResponse = null;

                while (retries < MAX_FILEPOINTER_RETRIES)
                {
                    RefreshToken();
                    Log.Debug("Requesting {0} file pointers, attempt {1}", POINTERS_PER_REQUEST, retries);

                    filePointerResponse = StorjRestClient.Request<List<FilePointer>>(new GetFilePointersRequest(BucketId, FileId, token.TokenKey, POINTERS_PER_REQUEST, pointersReceived));

                    if (filePointerResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Log.Debug("Requesting {0} file pointers failed, attempt {1}. Trying again in {2} seconds", POINTERS_PER_REQUEST, retries, POINTER_RETRY_DELAY_IN_S);
                        retries++;

                        if (retries < MAX_FILEPOINTER_RETRIES)
                        {
                            Thread.Sleep(POINTER_RETRY_DELAY_IN_S * 1000);
                            continue;
                        }

                        Log.Debug("Requesting {0} file pointers failed ultimately, after {1} attempts", POINTERS_PER_REQUEST, retries);
                        StorjClient.ThrowStorjResponseError(new GetFilePointerException(), filePointerResponse.Response);
                    }
                    else
                        break;
                }

                List<FilePointer> pointers = filePointerResponse.ToObject();

                if (pointers.Count == 0)
                    break;

                Log.Debug("Received {0} file pointers in this request", pointers.Count);
                pointersReceived += pointers.Count;

                allPointers.AddRange(pointers);
            } while (pointersReceived % POINTERS_PER_REQUEST == 0);

            //no file pointers found
            if (allPointers.Count == 0)
                throw new GetFilePointerException();

            Log.Debug("{0} file pointer received in total", pointersReceived);

            return allPointers;
        }

        private void ProgressUpdate()
        {
            if (ProgressEvent != null)
                ProgressEvent.BeginInvoke(new DownloadProgressEventArgs(bytesDownloaded, bytesToDownload), null, null);
        }
    }
}

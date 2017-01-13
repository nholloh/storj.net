using Storj.net.Model;
using Storj.net.Model.EventArgs;
using Storj.net.Model.Exception;
using Storj.net.Network;
using Storj.net.Network.Request;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storj.net.File
{
    class FileUploader
    {
        const string MIMETYPE = "application/octet-stream";
        const int DEFAULT_UPLOAD_THREADS = 10;
        const int MAX_UPLOAD_RETRIES = 5;

        /* HOW THIS WORKS:
         * 
         * 1) /buckets/{BucketId}/tokens --> get token for push
         * 2) retrieve token from storj bridge
         * 3) get optimal shard size (standard for now)
         * 4) create file staging frame
         * 5) encrypt & shard the file
         * 6) upload shard(s) to farmer(s) -> HTTP upload (POST) to http://farmer.address/shards/{Hash}?token={Token}/ with content application/octet-stream && header x-storj-node-id: {NodeId}
         * 7) register frame with bucket
         * 
         * 
         * REQUESTS:
         * 1) /buckets/{BucketId}/tokens (POST) -> get token for push
         * 2) /frames (POST) -> create new frame
         * 3) /frames/{FrameId} (PUT) -> register shard with frame
         * 4) 
         */



        public string BucketId { get; set; }
        public string Filename { get; set; }
        public string StorjFilename { get; set; }
        public int ConcurrentUploadThreads { get; set; }
        public Action<UploadProgressEventArgs> ProgressEvent { get; set; }
        public string Token { get { return token.TokenKey; } }


        private ShardingUtil sharder;
        private Token token;
        private Frame frame;
        private Cipher cipher;
        private string cryptFilename;
        private List<Thread> uploadWorkers = new List<Thread>();
        private long bytesToUpload = 0;
        private long bytesUploaded = 0;
        private bool uploadAborted = false;
        
        public FileUploader(string bucketId, string filename, Action<UploadProgressEventArgs> progressEvent, string storjFilename= "", string cipher = null)
        {
            this.BucketId = bucketId;
            this.Filename = filename;
            this.cryptFilename = filename + ".crypt";
            this.StorjFilename = storjFilename;
            this.ConcurrentUploadThreads = DEFAULT_UPLOAD_THREADS;
            this.cipher = (cipher == null ? null : Cipher.FromString(cipher));
        }

        public StorjFile Start()
        {
            //retrieve token
            RefreshToken();

            //create frame
            Log.Debug("Creating staging frame");
            StorjRestResponse<Frame> frameResponse = StorjRestClient.Request<Frame>(new CreateFrameRequest());
            if (frameResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new FrameCreationException(), frameResponse.Response);
            frame = frameResponse.ToObject();

            //if no storj file name has been specified: generate a random one (otherwise bridge might reject files due to same name although might have a different name locally)
            if (StorjFilename.Equals(""))
                StorjFilename = RandomStringUtil.GenerateRandomName();

            //encrypt file
            if (cipher == null)
            {
                cipher = CryptoUtil.GenerateCipher();
                KeyRingUtil.Store(StorjFilename, cipher);
            }
            Log.Debug("Encrypting file {0}", this.cryptFilename);
            CryptoUtil.Encrypt(this.Filename, this.cryptFilename, cipher);

            Log.Debug("Initializing sharder for file {0}", this.cryptFilename);
            sharder = new ShardingUtil(this.cryptFilename);

            bytesToUpload = sharder.ShardCount * StorjClient.ShardSize;

            if (sharder.ShardCount < ConcurrentUploadThreads)
                ConcurrentUploadThreads = sharder.ShardCount;

            //start workers
            for (int i = 0; i < ConcurrentUploadThreads; i++)
            {
                Thread worker = new Thread(new ThreadStart(UploadWorker));
                uploadWorkers.Add(worker);
                worker.Start();

                //give threads a short time to call the sharder for retrieving their shard
                Thread.Sleep(10);
            }

            //join workers to wait for them to complete uploading shards
            foreach (Thread worker in uploadWorkers)
            {
                if (uploadAborted)
                    break;
                worker.Join();
            }

            if (uploadAborted)
            {
                if (System.IO.File.Exists(cryptFilename))
                    System.IO.File.Delete(cryptFilename);

                throw new ShardUploadException();
            }

            if (System.IO.File.Exists(cryptFilename))
                System.IO.File.Delete(cryptFilename);

            //add frame to bucket
            //if not successful -> data will expire in the network
            Log.Debug("Adding frame {0} to bucket {1} with name {2}", frame.Id, this.BucketId, this.StorjFilename);
            StorjRestResponse<StorjFile> frameToBucketResponse = StorjRestClient.Request<StorjFile>(new StoreFileRequest(this.BucketId, frame.Id, MIMETYPE, this.StorjFilename));

            if (frameToBucketResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new AddFrameToBucketException(), frameToBucketResponse.Response);

            return frameToBucketResponse.ToObject();
        }

        private void RefreshToken()
        {
            Log.Debug("Retrieving token for operation");

            StorjRestResponse<Token> tokenResponse = StorjRestClient.Request<Token>(new CreateTokenRequest(BucketId, Model.Operation.PUSH));
            if (tokenResponse.StatusCode != System.Net.HttpStatusCode.Created)
                StorjClient.ThrowStorjResponseError(new TokenCreationException(), tokenResponse.Response);
            token = tokenResponse.ToObject();
        }

        private void UploadWorker()
        {
            Shard shard;
            while ((shard = sharder.NextShard()) != null)
            {
                int retries = 0;

                while (retries < MAX_UPLOAD_RETRIES)
                {
                    try
                    {
                        Log.Debug("Trying to upload shard {0}, attempt {1}", shard.Index, retries);
                        ProcessShard(shard);
                        bytesUploaded += StorjClient.ShardSize;
                        ProgressUpdate();
                        System.IO.File.Delete(shard.Path);
                        break;
                    } catch (Exception e)
                    {
                        Log.Debug("Upload of shard {0} failed, attempt {1}", shard.Index, retries);
                        retries++;
                        if (retries < MAX_UPLOAD_RETRIES)
                            continue;

                        Log.Debug("Upload of shard {0} ultimately failed, after {1} attempts", shard.Index, retries - 1);
                        uploadAborted = true;
                        Thread.CurrentThread.Abort();
                    }
                }
            }
        }

        private void ProcessShard(Shard shard)
        {
            Log.Debug("Requesting upload token for shard {0}", shard.Index);
            StorjRestResponse<ShardToken> shardTokenResponse = StorjRestClient.Request<ShardToken>(new AddShardRequest(
                        frame.Id, shard.Hash, shard.Size, shard.Index, shard.Challenges, shard.Tree, StorjClient.ExcludedFarmers));

            if (shardTokenResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new ShardUploadException(), shardTokenResponse.Response);

            Log.Debug("Requesting upload token for shard {0} successful. Starting upload to farmer", shard.Index);

            ShardToken shardToken = shardTokenResponse.ToObject();

            try
            {
                HttpShardTransferUtil.UploadShard(shard, shardToken);
            } catch (Exception e)
            {
                Log.Debug("Excluding bad farmer {0}", shardToken.Farmer.NodeId);
                StorjClient.ExcludedFarmers.Add(shardToken.Farmer.NodeId);
                throw e;
            }

            Log.Debug("Upload of shard {0} successfully completed", shard.Index);
        }

        private void ProgressUpdate()
        {
            if (ProgressEvent != null)
                ProgressEvent.BeginInvoke(new UploadProgressEventArgs(bytesUploaded, bytesToUpload), null, null);
        }
    }
}

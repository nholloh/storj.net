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
    class EmptyFrameCreator
    {
        const string MIMETYPE = "application/octet-stream";

        /* HOW THIS WORKS:
         * 
         * 1) /buckets/{BucketId}/tokens --> get token for push
         * 2) retrieve token from storj bridge
         * 4) create file staging frame
         * 7) register frame with bucket
         * 
         */

        public string BucketId { get; set; }
        public string StorjFilename { get; set; }
        public string Token { get { return token.TokenKey; } }


        private Token token;
        private Frame frame;
        
        public EmptyFrameCreator(string bucketId, string storjFilename)
        {
            this.BucketId = bucketId;
            this.StorjFilename = storjFilename;
        }

        public StorjFile Start()
        {
            //retrieve token
            StorjRestResponse<Token> tokenResponse = StorjRestClient.Request<Token>(new CreateTokenRequest(BucketId, Model.Operation.PUSH));
            if (tokenResponse.StatusCode != System.Net.HttpStatusCode.Created)
                StorjClient.ThrowStorjResponseError(new TokenCreationException(), tokenResponse.Response);
            token = tokenResponse.ToObject();

            //create frame
            StorjRestResponse<Frame> frameResponse = StorjRestClient.Request<Frame>(new CreateFrameRequest());
            if (frameResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new FrameCreationException(), tokenResponse.Response);
            frame = frameResponse.ToObject();

            //if no storj file name has been specified: generate a random one (otherwise bridge might reject files due to same name although might have a different name locally)
            if (StorjFilename.Equals(""))
                throw new FrameCreationException();

            //add frame to bucket
            //if not successful -> data will expire in the network
            StorjRestResponse<StorjFile> frameToBucketResponse = StorjRestClient.Request<StorjFile>(new StoreFileRequest(this.BucketId, frame.Id, MIMETYPE, this.StorjFilename));
            if (frameToBucketResponse.StatusCode != System.Net.HttpStatusCode.OK)
                StorjClient.ThrowStorjResponseError(new AddFrameToBucketException(), frameToBucketResponse.Response);

            return frameToBucketResponse.ToObject();
        }
    }
}

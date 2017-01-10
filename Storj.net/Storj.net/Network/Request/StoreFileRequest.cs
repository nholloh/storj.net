using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}/files", RestSharp.Method.POST)]
    class StoreFileRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        [JsonProperty("frame")]
        public string FrameId { get; set; }

        [JsonProperty("mimetype")]
        public string Mimetype { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        public StoreFileRequest(string bucketId, string frameId, string mimetype, string filename)
        {
            this.BucketId = bucketId;
            this.FrameId = frameId;
            this.Mimetype = mimetype;
            this.Filename = filename;
        }
    }
}

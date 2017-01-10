using Newtonsoft.Json;
using Storj.net.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}/tokens", RestSharp.Method.POST)]
    class CreateTokenRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("file")]
        public string FileId { get; set; }

        public CreateTokenRequest(string bucketId, Operation operation, string fileId = "")
        {
            this.BucketId = bucketId;
            this.Operation = operation.ToString();
            this.FileId = fileId;
        }
    }
}

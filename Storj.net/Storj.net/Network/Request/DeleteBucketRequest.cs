using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}", RestSharp.Method.DELETE)]
    class DeleteBucketRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        public DeleteBucketRequest(string bucketId)
        {
            this.BucketId = bucketId;
        }
    }
}

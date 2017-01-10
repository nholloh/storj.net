using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}", RestSharp.Method.GET)]
    class GetBucketRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        public GetBucketRequest(string bucketId)
        {
            this.BucketId = bucketId;
        }
    }
}

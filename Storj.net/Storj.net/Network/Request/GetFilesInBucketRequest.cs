using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}/files", RestSharp.Method.GET)]
    class GetFilesInBucketRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        public GetFilesInBucketRequest(string bucketId)
        {
            this.BucketId = bucketId;
        }
    }
}

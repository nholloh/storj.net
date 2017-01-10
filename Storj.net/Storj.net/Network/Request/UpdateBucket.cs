using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}", RestSharp.Method.PATCH)]
    class UpdateBucket : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        [JsonProperty("pubkeys")]
        public List<string> PublicKeys { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public UpdateBucket(string bucketId, string name, List<string> publicKeys)
        {
            this.BucketId = bucketId;
            this.Name = name;
            this.PublicKeys = publicKeys;
        }
    }
}

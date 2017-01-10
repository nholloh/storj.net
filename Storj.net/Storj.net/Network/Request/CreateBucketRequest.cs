using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets", RestSharp.Method.POST)]
    class CreateBucketRequest : StorjRestRequest
    {
        [JsonProperty("pubkeys")]
        public List<string> PublicKeys { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public CreateBucketRequest(string name, List<string> publicKeys)
        {
            // TODO: find out how pub keys for buckets look like
            this.Name = name;
            this.PublicKeys = publicKeys;
        }
    }
}

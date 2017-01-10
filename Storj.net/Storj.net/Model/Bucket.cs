using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    public class Bucket
    {
        [JsonProperty("storage")]
        public int Storage { get; set; }

        [JsonProperty("transfer")]
        public int Transfer { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("pubkeys")]
        public List<string> PublicKeys { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}

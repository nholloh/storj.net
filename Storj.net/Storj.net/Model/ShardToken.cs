using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class ShardToken
    {
        [JsonProperty("token")]
        public string TokenKey { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("farmer")]
        public Contact Farmer { get; set; }
    }
}

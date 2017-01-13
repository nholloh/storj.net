using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class FilePointer
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("operation")]
        public Operation Operation { get; set; }

        [JsonProperty("farmer")]
        public Contact Farmer { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }
    }
}

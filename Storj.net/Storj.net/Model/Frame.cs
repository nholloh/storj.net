using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class Frame
    {
        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("shards")]
        public List<string> Shards { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("locked")]
        public bool Locked { get; set; }
    }
}

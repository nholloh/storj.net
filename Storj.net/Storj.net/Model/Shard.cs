using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class Shard
    {
        public string Id { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        public string Path { get; set; }

        [JsonProperty("tree")]
        public List<string> Tree { get { return tree; } set { tree = value; } }
        private List<string> tree = new List<string>();

        [JsonProperty("challenges")]
        public List<string> Challenges { get { return challenges; } set { tree = value; } }
        private List<string> challenges = new List<string>();
    }
}

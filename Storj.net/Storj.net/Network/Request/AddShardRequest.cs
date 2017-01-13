using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/frames/{FrameId}", RestSharp.Method.PUT)]
    class AddShardRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string FrameId { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("challenges")]
        public List<string> Challenges { get; set; }

        [JsonProperty("tree")]
        public List<string> Tree { get; set; }

        [JsonProperty("exclude")]
        public List<string> Exclude { get; set; }

        public AddShardRequest(string frameId, string hash, long size, int index, List<string> challenges, List<string> tree, List<string> exclude = null)
        {
            this.FrameId = frameId;
            this.Hash = hash;
            this.Size = size;
            this.Index = index;
            this.Challenges = challenges;
            this.Tree = tree;
            this.Exclude = (exclude == null ? new List<string>() : exclude);
        }
    }
}

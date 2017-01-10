using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class FileAuthorization
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("hash")]
        public string Hash { get; private set; }

        [JsonProperty("operation")]
        public Operation Operation { get; private set; }

        public FileAuthorization() { }

        [JsonConstructor]
        public FileAuthorization(string token, string hash, Operation operation)
        {
            this.Token = token;
            this.Hash = hash;
            this.Operation = operation;
        }

    }
}

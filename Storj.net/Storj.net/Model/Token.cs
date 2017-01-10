using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class Token
    {
        [JsonProperty("bucket")]
        public string BucketId { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        [JsonProperty("operation")]
        public Operation Operation { get; set; }

        [JsonProperty("token")]
        public string TokenKey { get; set; }

        [JsonProperty("encryptionKey")]
        public string EncryptionKey { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }
    }
}

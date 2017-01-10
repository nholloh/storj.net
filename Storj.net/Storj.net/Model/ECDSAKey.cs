using Newtonsoft.Json;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    public class ECDSAKey
    {
        [JsonProperty("key")]
        public string PublicKey { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        public ECDSAKey() { }
    }
}

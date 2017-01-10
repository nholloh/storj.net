using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/keys", RestSharp.Method.POST)]
    class RegisterKeyRequest : StorjRestRequest
    {
        [JsonProperty("key")]
        public string PublicKey { get; set; }

        public RegisterKeyRequest(string publicKey)
        {
            PublicKey = publicKey;
        }
    }
}

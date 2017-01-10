using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/keys/{PublicKey}", RestSharp.Method.DELETE)]
    class DeleteKeyRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string PublicKey { get; set; }

        public DeleteKeyRequest(string publicKey)
        {
            this.PublicKey = publicKey;
        }
    }
}

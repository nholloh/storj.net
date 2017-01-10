using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/contacts/{NodeId}", RestSharp.Method.GET)]
    class GetContactRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string NodeId { get; set; }

        public GetContactRequest(string nodeId)
        {
            this.NodeId = nodeId;
        }
    }
}

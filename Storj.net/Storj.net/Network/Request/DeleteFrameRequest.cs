using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/frames/{FrameId}", RestSharp.Method.DELETE)]
    class DeleteFrameRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string FrameId { get; set; }

        public DeleteFrameRequest(string frameId)
        {
            this.FrameId = frameId;
        }
    }
}

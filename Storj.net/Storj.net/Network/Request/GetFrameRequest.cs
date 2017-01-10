using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/frames/{FrameId}", RestSharp.Method.GET)]
    class GetFrameRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string FrameId { get; set; }

        public GetFrameRequest(string frameId)
        {
            this.FrameId = frameId;
        }
    }
}

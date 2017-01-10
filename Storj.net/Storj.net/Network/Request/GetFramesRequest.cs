using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/frames", RestSharp.Method.GET)]
    class GetFramesRequest : StorjRestRequest
    {
    }
}

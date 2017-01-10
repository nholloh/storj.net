using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/contacts", RestSharp.Method.GET)]
    class GetContactsRequest : StorjRestRequest
    {
    }
}

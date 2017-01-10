using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    class EndpointAttribute : Attribute
    {
        public string Endpoint { get; private set; }
        public Method Method { get; private set; }

        public EndpointAttribute(string endpoint, Method method)
        {
            this.Endpoint = endpoint;
            this.Method = method;
        }
    }
}

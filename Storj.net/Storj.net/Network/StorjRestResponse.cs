using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    class StorjRestResponse<T>
    {
        public IRestResponse Response { get; private set; }

        public string Content { get { return Response.Content; } }

        public HttpStatusCode StatusCode { get { return Response.StatusCode; } }

        public StorjRestResponse(IRestResponse response)
        {
            this.Response = response;
        }

        public JObject ToJObject()
        {
            return JObject.Parse(Content);
        }

        public T ToObject()
        {
            if (!typeof(T).Namespace.Equals("System.Collections.Generic"))
                return (T)JObject.Parse(Content).ToObject(typeof(T));
            else
                return (T)JArray.Parse(Content).ToObject(typeof(T));
        }

        public JArray ToJArray()
        {
            return JArray.Parse(Content);
        }


    }
}

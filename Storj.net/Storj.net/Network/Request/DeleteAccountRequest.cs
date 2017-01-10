using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/users/{Email}", RestSharp.Method.DELETE)]
    class DeleteAccountRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string Email { get; set; }

        [JsonProperty("redirect")]
        public string Redirect { get; set; }

        public DeleteAccountRequest(string email, string redirect = "")
        {
            this.Email = email;
            this.Redirect = redirect;
        }
    }
}

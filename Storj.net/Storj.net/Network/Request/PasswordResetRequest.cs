using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/users/{Email}", RestSharp.Method.PATCH)]
    class PasswordResetRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string NewPassword { get; set; }

        [JsonProperty("redirect")]
        public string Redirect { get; set; }

        public PasswordResetRequest(string email, string newPassword, string redirect = "")
        {
            this.Email = email;
            this.NewPassword = newPassword;
            this.Redirect = redirect;
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/users", RestSharp.Method.POST)]
    class CreateUserRequest : StorjRestRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("pubkey")]
        public string PublicKey { get; set; }

        public CreateUserRequest(string email, string password, string publicKey)
        {
            this.Email = email;
            this.Password = password;
            this.PublicKey = publicKey;
        }
    }
}

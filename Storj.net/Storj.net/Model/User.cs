using Newtonsoft.Json;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class User
    {
        [JsonProperty("email")]
        public string Email { get; set; }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = HashUtil.SHA256EncryptToString(value);
            }
        }
        private string password = "";

        [JsonProperty("redirect")]
        public string Redirect { get; set; }

        [JsonProperty("__nonce")]
        public long Nonce { get; set; }

        public bool Activated { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("pubkey")]
        public string PublicKey { get; set; }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    public class Cipher
    {
        public string Passphrase { get; private set; }
        public byte[] Iv { get; private set; }
        public byte[] Salt { get; private set; }

        public Cipher(string passphrase, byte[] iv, byte[] salt)
        {
            this.Passphrase = passphrase;
            this.Iv = iv;
            this.Salt = salt;
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public static Cipher FromString(string value)
        {
            JObject obj = JObject.Parse(value);
            return (Cipher)obj.ToObject(typeof(Cipher));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    class HeaderAttribute : Attribute
    {
        public List<KeyValuePair<string, string>> Attributes { get; private set; }

        public HeaderAttribute(params string[] attributes)
        {
            if (attributes.Length % 2 != 0)
                return;

            this.Attributes = new List<KeyValuePair<string, string>>();

            for (int i = 0; i < attributes.Length; i+=2)
                this.Attributes.Add(new KeyValuePair<string, string>(attributes[i], attributes[i + 1]));
        }
    }
}

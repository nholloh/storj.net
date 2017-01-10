using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    class DefaultAttribute : System.Attribute
    {
        public object Value { get; private set; }

        public DefaultAttribute(object value)
        {
            this.Value = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    class PrerequisitesAttribute : Attribute
    {
        public Prerequisite[] Prerequisites { get; private set; }

        public PrerequisitesAttribute(params Prerequisite[] prerequisites)
        {
            this.Prerequisites = prerequisites;
        }
    }

    enum Prerequisite
    {
        LOGGED_IN,
        LOGGED_OUT,
        AUTHENTICATED
    }
}

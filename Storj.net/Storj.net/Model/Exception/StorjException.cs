﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model.Exception
{
    public class StorjException : System.Exception
    {
        public new string Message { get; set; }
    }
}

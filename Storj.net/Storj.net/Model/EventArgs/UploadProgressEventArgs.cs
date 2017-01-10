using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model.EventArgs
{
    public class UploadProgressEventArgs
    {
        public double Progress { get; private set; }
        public long BytesToSend { get; private set; }
        public long BytesSent { get; private set; }

        public UploadProgressEventArgs(long bytesSent, long bytesToSend)
        {
            this.BytesSent = bytesSent;
            this.BytesToSend = bytesToSend;
            this.Progress = bytesSent / bytesToSend * 100;
        }
    }
}

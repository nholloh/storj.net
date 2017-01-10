using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model.EventArgs
{
    public class DownloadProgressEventArgs
    {
        public double Progress { get; private set; }
        public long BytesToReceive { get; private set; }
        public long BytesReceived { get; private set; }

        public DownloadProgressEventArgs(long bytesReceived, long bytesToReceive)
        {
            this.BytesReceived = bytesReceived;
            this.BytesToReceive = bytesToReceive;
            this.Progress = bytesReceived / bytesToReceive * 100;
        }
    }
}

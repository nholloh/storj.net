using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    public class StorjFile
    {
        [JsonProperty("frame")]
        public string Frame { get; set; }

        [JsonProperty("mimetype")]
        public string Mimetype { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("id")]
        public string FileId { get; set; }

        public StorjFile() { }

        public StorjFile(string frame, string mimetype, string filename)
        {
            this.Frame = frame;
            this.Mimetype = mimetype;
            this.Filename = filename;
        }
    }
}

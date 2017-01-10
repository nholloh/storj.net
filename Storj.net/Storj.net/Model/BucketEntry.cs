using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Model
{
    class BucketEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("bucket")]
        public string Bucket { get; set; }

        [JsonProperty("mimetype")]
        public string Mimetype { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("frame")]
        public string Frame { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("renewal")]
        public string Renewal { get; set; }
    }
}

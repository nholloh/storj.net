using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}/files/{FileId}", RestSharp.Method.DELETE)]
    class DeleteFileRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        [JsonIgnore]
        public string FileId { get; set; }

        public DeleteFileRequest(string bucketId, string fileId)
        {
            this.BucketId = bucketId;
            this.FileId = fileId;
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network.Request
{
    [Endpoint("/buckets/{BucketId}/files/{FileId}?skip=\"{Skip}\"&limit=\"{Limit}\"", RestSharp.Method.GET)]
    [Header("x-token", "{XToken}")]
    class GetFilePointersRequest : StorjRestRequest
    {
        [JsonIgnore]
        public string BucketId { get; set; }

        [JsonIgnore]
        public string FileId { get; set; }

        [JsonIgnore]
        public string Skip { get; set; }

        [JsonIgnore]
        public string Limit { get; set; }

        [JsonIgnore]
        public string XToken { get; set; }

        public GetFilePointersRequest(string bucketId, string fileId, string xToken, int limit, int skip = 0)
        {
            this.BucketId = bucketId;
            this.FileId = fileId;
            this.Skip = skip.ToString();
            this.Limit = limit.ToString();
            this.XToken = xToken;
        }
    }
}

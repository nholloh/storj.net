using Storj.net.Model;
using Storj.net.Model.Exception;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class HttpShardTransferUtil
    {
        internal static void UploadShard(Shard shard, ShardToken token)
        {
            string endpoint = "http://" + token.Farmer.Address + ":" + token.Farmer.Port.ToString() + "/shards/" + shard.Hash + "?token=" + token.TokenKey;
            WebRequest request = HttpWebRequest.Create(endpoint);

            //set header
            request.Method = "POST";
            request.Headers.Add("x-storj-node-id", token.Farmer.NodeId);
            request.ContentType = "application/octet-stream";
            request.ContentLength = shard.Size;

            Stream requestStream = request.GetRequestStream();
            AdvFileStream shardStream = new AdvFileStream(shard.Path, FileMode.Open);

            shardStream.CopyTo(requestStream);
            shardStream.Close();
            requestStream.Flush();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            requestStream.Close();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ShardUploadException();

            response.Close();
        }

        internal static void DownloadShard(FilePointer ptr, string localPath)
        {
            //http://farmer.address/shards/{Hash}?token={Token}/ with content application/octet-stream && header x-storj-node-id: {NodeId}
            string endpoint = "http://" + ptr.Farmer.Address + ":" + ptr.Farmer.Port.ToString() + "/shards/" + ptr.Hash + "?token=" + ptr.Token;

            WebRequest request = HttpWebRequest.Create(endpoint);

            //set header
            request.Method = "GET";
            request.Headers.Add("x-storj-node-id", ptr.Farmer.NodeId);
            request.ContentType = "application/octet-stream";

            AdvFileStream shardStream = new AdvFileStream(localPath, FileMode.Create);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ShardUploadException();

            response.GetResponseStream().CopyTo(shardStream);
            shardStream.Close();
            response.Close();
        }
    }
}

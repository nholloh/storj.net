using Storj.net.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    /* 
     * original java code by Stephen Nutbrown 23/07/2016
     * ported to C# and modified to support concurrent uploads by Niklas Holloh on 08/01/2017
     */
    class ShardingUtil
    {
        static ShardingUtil()
        {
            if (!Directory.Exists(StorjClient.ShardDirectory))
                Directory.CreateDirectory(StorjClient.ShardDirectory);
        }

        public int ShardCount { get; private set; }
        public FileInfo File { get; private set; }
        public long FileLength { get; private set; }

        private string fileName;
        private int shardIndex = 0;
        private Dictionary<int, Shard> shards = new Dictionary<int, Shard>();

        internal ShardingUtil(string fileName)
        {
            this.fileName = fileName;
            this.File = new FileInfo(fileName);
            if (!this.File.Exists)
                return;

            this.FileLength = File.Length;
            this.ShardCount = (int)Math.Ceiling((decimal)File.Length / (StorjClient.ShardSize - 4));
        }

        /// <summary>
        /// Generates the next shard or returns null
        /// </summary>
        /// <returns>A shard object or null if all shards have been created.</returns>
        internal Shard NextShard()
        {
            // Save current shard index so that concurrent threads calling NextShard will retrieve the correct shard without interfering with this process.
            // This allows for multiple parallel upload workers for a single file.
            int tempShardIndex = shardIndex;
            
            long streamPosition = (StorjClient.ShardSize - 4) * tempShardIndex;

            if (streamPosition > new FileInfo(fileName).Length)
                return null;

            shardIndex++;

            AdvFileStream input = new AdvFileStream(fileName, FileMode.Open);
            string shardFileName = Path.Combine(StorjClient.ShardDirectory, RandomStringUtil.GenerateRandomName() + ".shard");
            AdvFileStream shardStream = new AdvFileStream(shardFileName, FileMode.Create);

            input.Position = streamPosition;

            //first 4 byte of shard are shard index
            byte[] shardData = input.Read(StorjClient.ShardSize - 4);
            byte[] buffer = BitConverter.GetBytes(tempShardIndex).Concat(shardData).ToArray();
            //byte[] buffer = input.Read(StorjClient.ShardSize);
            input.Close();

            shardStream.Write(buffer);

            Shard shard = new Shard()
            {
                Size = buffer.Length,
                Hash = HashUtil.RipemdSHA256EncryptFile(shardStream),
                Path = shardFileName,
                Index = tempShardIndex
            };

            shardStream.Close();

            CreateShardChallenges(shard, buffer, StorjClient.ChallengesPerShard);

            shards.Add(tempShardIndex, shard);

            return shard;
        }

        private void CreateShardChallenges(Shard shard, byte[] data, int challenges)
        {
            for (int i = 0; i < challenges; i++)
            {
                string challengeString = RandomStringUtil.GenerateRandomChallengeString();
                byte[] challenge = Encoding.UTF8.GetBytes(challengeString);

                // trim empty space at the end of data
                byte[] trimmedData = new byte[shard.Size];
                Array.Copy(data, trimmedData, shard.Size);

                byte[] challengeShardData = challenge.Concat(trimmedData).ToArray();

                // Decode challengeShardData and then back to byte[] with ASCII again (HexEncode)
                byte[] hexChallengeShardData = HashUtil.HexEncode(challengeShardData);

                // double hash the entire thing
                // RMD160(SHA256(RMD160(SHA256(challenge + shard))))
                byte[] tree = HashUtil.RipemdSHA256Encrypt(HashUtil.RipemdSHA256Encrypt(hexChallengeShardData));

                shard.Challenges.Add(challengeString);
                shard.Tree.Add(Encoding.ASCII.GetString(tree));
            }
        }

        internal void AppendShard(string shardFileName)
        {
            AdvFileStream shardStream = new AdvFileStream(shardFileName, FileMode.Open);
            int shardIndex = shardStream.ReadInt();

            shards.Add(shardIndex, new Shard()
            {
                Index = shardIndex,
                Path = shardFileName
            });

            shardStream.Close();

            if (!shards.ContainsKey(this.shardIndex))
                return;

            AdvFileStream target = new AdvFileStream(this.fileName, FileMode.OpenOrCreate);

            for (this.shardIndex = this.shardIndex; this.shardIndex < shards.Count; this.shardIndex++)
            {
                if (!shards.ContainsKey(this.shardIndex))
                    break;
                AppendShardToFile(this.shardIndex, target);
            }

            target.Close();
        }

        private void AppendShardToFile(int index, AdvFileStream target)
        {
            AdvFileStream shardStream = new AdvFileStream(shards[index].Path, FileMode.Open);
            
            // skip first four bytes as they are index only
            shardStream.Position = 4;

            // read shardStream to end and write the data to target
            target.Write(shardStream.Read(shardStream.Length - 4));
            shardStream.Close();

            System.IO.File.Delete(shards[index].Path);
        }

        internal void FinalizeFile()
        {
            // read last shard data again
            AdvFileStream target = new AdvFileStream(this.fileName, FileMode.Open);
            target.Position = target.Length - (StorjClient.ShardSize - 4);

            byte[] lastShardData = target.Read(StorjClient.ShardSize - 4);

            // truncate trailing zeroes
            int lastIndex = Array.FindLastIndex(lastShardData, b => b != 0);
            Array.Resize(ref lastShardData, lastIndex + 1);

            // reset position to beginning of this shard and write truncated data
            target.Position = target.Length - (StorjClient.ShardSize - 4);
            target.Write(lastShardData);
            target.SetLength(target.Length - (StorjClient.ShardSize - 4) + lastIndex + 1);
            target.Close();
        }
    }
}

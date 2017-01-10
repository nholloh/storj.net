using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class HashUtil
    {
        [DebuggerStepThrough]
        public static string SHA256EncryptToString(string value)
        {
            return Encoding.UTF8.GetString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        [DebuggerStepThrough]
        public static string SHA256EncryptToHex(string value)
        {
            return HexEncodeToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        [DebuggerStepThrough]
        public static string SHA256EncryptToHex(byte[] value)
        {
            return HexEncodeToString(SHA256.Create().ComputeHash(value));
        }

        [DebuggerStepThrough]
        public static byte[] SHA256Encrypt(string value)
        {
            return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value));
        }

        [DebuggerStepThrough]
        public static byte[] SHA256Encrypt(byte[] value)
        {
            return SHA256.Create().ComputeHash(value);
        }

        [DebuggerStepThrough]
        public static string RipemdSHA256EncryptFile(string fileName)
        {
            AdvFileStream file = new AdvFileStream(fileName, FileMode.Open);
            string hash = RipemdSHA256EncryptFile(file);
            file.Close();
            return hash;
        }

        [DebuggerStepThrough]
        public static string RipemdSHA256EncryptFile(AdvFileStream file)
        {
            // set to beginning of file
            file.Position = 0;
            byte[] sha256Hash = SHA256.Create().ComputeHash(file);
            
            // get the RipeMD160 for the hash
            // Convert to hex string and remove dashes
            // Convert back to byte array using ASCII encoding
            return BitConverter.ToString(RIPEMD160Managed.Create().ComputeHash(sha256Hash)).Replace("-", "").ToLower();
        }

        [DebuggerStepThrough]
        public static byte[] RipemdSHA256Encrypt(byte[] data)
        {
            byte[] sha256Hash = HexEncode(SHA256.Create().ComputeHash(data));
            return HexEncode(RIPEMD160Managed.Create().ComputeHash(sha256Hash));
        }

        [DebuggerStepThrough]
        public static byte[] HexEncode(byte[] data)
        {
            return Encoding.ASCII.GetBytes(BitConverter.ToString(data).Replace("-", "").ToLower());
        }

        [DebuggerStepThrough]
        public static string HexEncodeToString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "").ToLower();
        }

        [DebuggerStepThrough]
        public static byte[] HexDecode(string data)
        {
            byte[] result = new byte[data.Length / 2];

            for (int i = 0; i < result.Count(); i++)
                result[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);

            return result;
        }
    }
}

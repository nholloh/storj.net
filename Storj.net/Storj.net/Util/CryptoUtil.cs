using Storj.net.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class CryptoUtil
    {
        /* 
         * Encrypt and Decrypt code by:
         * CraigTP, StackOverFlow
         * http://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
         * 
         * Modified by Niklas Holloh, 10.01.2016
         * 
         */

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int KEYSIZE = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DERIVATION_ITERATIONS = 1000;

        // The size of the buffer the file stream will read from the source stream, encrypt and write to the target stream per iteration.
        // More means faster processing due to less IO access but more memory consumption.
        // Set to 10 MiB.
        private const int STREAM_BUFFER_SIZE = 10485760;

        internal static void Encrypt(string sourceFile, string targetFile, Cipher cipher)
        {
            AdvFileStream source = new AdvFileStream(sourceFile, FileMode.Open);
            AdvFileStream target;

            try
            {
                if (System.IO.File.Exists(targetFile))
                    System.IO.File.Delete(targetFile);

                target = new AdvFileStream(targetFile, FileMode.Create);
            }
            catch (Exception e)
            {
                source.Close();
                throw e;
            }

            try
            {
                using (var password = new Rfc2898DeriveBytes(cipher.Passphrase, cipher.Salt, DERIVATION_ITERATIONS))
                {
                    var keyBytes = password.GetBytes(KEYSIZE / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, cipher.Iv))
                        {
                            using (var cryptoStream = new CryptoStream(target, encryptor, CryptoStreamMode.Write))
                            {
                                while (source.Position + STREAM_BUFFER_SIZE < source.Length)
                                {
                                    byte[] buffer = source.Read(STREAM_BUFFER_SIZE);
                                    cryptoStream.Write(buffer, 0, buffer.Length);
                                }

                                if (source.Position < source.Length)
                                {
                                    int remainingBytes = (int)(source.Length - source.Position);
                                    byte[] buffer = source.Read(remainingBytes);
                                    cryptoStream.Write(buffer, 0, buffer.Length);
                                }

                                cryptoStream.FlushFinalBlock();
                                source.Close();
                                target.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CleanUp("", targetFile);
                throw e;
            }

            if (System.IO.File.Exists(targetFile))
                System.IO.File.SetAttributes(targetFile, System.IO.File.GetAttributes(targetFile) | FileAttributes.Hidden);
        }

        internal static void Decrypt(string sourceFile, string targetFile, Cipher cipher)
        {
            AdvFileStream source = new AdvFileStream(sourceFile, FileMode.Open);
            AdvFileStream target;

            try
            {
                if (System.IO.File.Exists(targetFile))
                    System.IO.File.Delete(targetFile);

                target = new AdvFileStream(targetFile, FileMode.Create);
            }
            catch (Exception e)
            {
                source.Close();
                throw e;
            }

            try
            {
                using (var password = new Rfc2898DeriveBytes(cipher.Passphrase, cipher.Salt, DERIVATION_ITERATIONS))
                {
                    var keyBytes = password.GetBytes(KEYSIZE / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, cipher.Iv))
                        {
                            using (var cryptoStream = new CryptoStream(source, decryptor, CryptoStreamMode.Read))
                            {
                                while (source.Position + STREAM_BUFFER_SIZE < source.Length)
                                {
                                    byte[] buffer = new byte[STREAM_BUFFER_SIZE];
                                    cryptoStream.Read(buffer, 0, STREAM_BUFFER_SIZE);

                                    target.Write(buffer);
                                }

                                if (source.Position < source.Length)
                                {
                                    int remainingBytes = (int)(source.Length - source.Position);

                                    byte[] buffer = new byte[remainingBytes];
                                    cryptoStream.Read(buffer, 0, remainingBytes);

                                    target.Write(buffer);
                                }

                                cryptoStream.Close();
                                source.Close();
                                target.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                source.Close();
                target.Close();
                CleanUp(sourceFile, targetFile);
                throw e;
            }
        }

        private static void CleanUp(string source, string target)
        {
            if (System.IO.File.Exists(source))
                System.IO.File.Delete(source);

            if (System.IO.File.Exists(target))
                System.IO.File.Delete(target);
        }

        internal static void EncryptToFile(string data, string filepath, Cipher cipher)
        {
            MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(data));
            AdvFileStream target = new AdvFileStream(filepath, FileMode.Create);

            using (var password = new Rfc2898DeriveBytes(cipher.Passphrase, cipher.Salt, DERIVATION_ITERATIONS))
            {
                var keyBytes = password.GetBytes(KEYSIZE / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, cipher.Iv))
                    {
                        using (var cryptoStream = new CryptoStream(target, encryptor, CryptoStreamMode.Write))
                        {
                            while (source.Position + STREAM_BUFFER_SIZE < source.Length)
                            {
                                byte[] buffer = new byte[STREAM_BUFFER_SIZE];
                                source.Read(buffer, 0, STREAM_BUFFER_SIZE);
                                cryptoStream.Write(buffer, 0, buffer.Length);
                            }

                            if (source.Position < source.Length)
                            {
                                int remainingBytes = (int)(source.Length - source.Position);
                                byte[] buffer = new byte[remainingBytes];
                                source.Read(buffer, 0, remainingBytes);
                                cryptoStream.Write(buffer, 0, buffer.Length);
                            }

                            cryptoStream.FlushFinalBlock();
                            cryptoStream.Close();
                            source.Close();
                            target.Close();
                        }
                    }
                }
            }
        }

        internal static string DecryptFromFile(string filepath, Cipher cipher)
        {
            AdvFileStream source = new AdvFileStream(filepath, FileMode.Open);
            string target = "";

            using (var password = new Rfc2898DeriveBytes(cipher.Passphrase, cipher.Salt, DERIVATION_ITERATIONS))
            {
                var keyBytes = password.GetBytes(KEYSIZE / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, cipher.Iv))
                    {
                        using (var cryptoStream = new CryptoStream(source, decryptor, CryptoStreamMode.Read))
                        {
                            while (source.Position + STREAM_BUFFER_SIZE < source.Length)
                            {
                                byte[] buffer = new byte[STREAM_BUFFER_SIZE];
                                cryptoStream.Read(buffer, 0, STREAM_BUFFER_SIZE);

                                target += Encoding.UTF8.GetString(buffer);
                            }

                            if (source.Position < source.Length)
                            {
                                int remainingBytes = (int)(source.Length - source.Position);

                                byte[] buffer = new byte[remainingBytes];
                                cryptoStream.Read(buffer, 0, remainingBytes);

                                target += Encoding.UTF8.GetString(buffer);
                            }

                            source.Close();
                            cryptoStream.Close();
                        }
                    }
                }
            }

            return target;
        }

        internal static Cipher GenerateCipherFromPassword(string password)
        {
            byte[] iv = HashUtil.SHA256Encrypt(password);
            byte[] salt = HashUtil.SHA256Encrypt(HashUtil.RipemdSHA256Encrypt(iv));

            return new Cipher(password, iv, salt);
        }

        internal static Cipher GenerateCipher()
        {
            return new Cipher(
                Encoding.UTF8.GetString(Generate256BitsOfRandomEntropy()),
                Generate256BitsOfRandomEntropy(),
                Generate256BitsOfRandomEntropy());
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}

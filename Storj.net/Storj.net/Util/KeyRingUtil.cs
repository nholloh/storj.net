using Newtonsoft.Json.Linq;
using Storj.net.Model;
using Storj.net.Model.Exception;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class KeyRingUtil
    {
        static dynamic decryptedKeyRing;
        static Cipher passphraseCipher;

        internal static Cipher Get(string name)
        {
            Load();

            if (decryptedKeyRing[name] == null)
                return null;

            Cipher cipher = Cipher.FromString(decryptedKeyRing[name].Value);
            Save();
            return cipher;
        }

        internal static void Store(string name, Cipher cipher)
        {
            Load();
            decryptedKeyRing[name] = cipher.ToString();
            Save();
        }

        internal static void Delete()
        {
            if (System.IO.File.Exists(StorjClient.KeyRingFile))
                System.IO.File.Delete(StorjClient.KeyRingFile);

            decryptedKeyRing = null;
        }

        private static void Load()
        {
            if (StorjClient.KeyRingPassphrase == null)
                throw new InvalidKeyRingPassphraseException();

            if (passphraseCipher == null)
                passphraseCipher = CryptoUtil.GenerateCipherFromPassword(StorjClient.KeyRingPassphrase);

            if (!System.IO.File.Exists(StorjClient.KeyRingFile))
            {
                decryptedKeyRing = JObject.Parse("{}");
                return;
            }

            try
            {
                string jsonKeyRing = CryptoUtil.DecryptFromFile(StorjClient.KeyRingFile, passphraseCipher);
                decryptedKeyRing = JObject.Parse(jsonKeyRing);
            } catch (Exception e)
            {
                throw new InvalidKeyRingPassphraseException();
            }
        }

        private static void Save()
        {
            if (StorjClient.KeyRingPassphrase == null)
                throw new InvalidKeyRingPassphraseException();

            if (passphraseCipher == null)
                passphraseCipher = CryptoUtil.GenerateCipherFromPassword(StorjClient.KeyRingPassphrase);

            string jsonKeyRing = JObject.FromObject(decryptedKeyRing).ToString();
            CryptoUtil.EncryptToFile(jsonKeyRing, StorjClient.KeyRingFile, passphraseCipher);

            decryptedKeyRing = null;
        }
    }
}

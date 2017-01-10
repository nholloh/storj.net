using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using RestSharp;
using Storj.net.File;
using Storj.net.Model;
using Storj.net.Model.Attribute;
using Storj.net.Model.EventArgs;
using Storj.net.Model.Exception;
using Storj.net.Network;
using Storj.net.Network.Request;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net
{
    public class StorjClient
    {
        #region init
        /// <summary>
        /// Startup to initialize all fields with default values.
        /// </summary>
        static StorjClient()
        {
            foreach (Type type in Assembly.GetAssembly(typeof(StorjClient)).GetTypes())
            {
                foreach (PropertyInfo property in type.GetProperties())
                {
                    if (property.GetCustomAttributes(typeof(DefaultAttribute)).Count() == 0)
                        continue;

                    object value = ((DefaultAttribute)property.GetCustomAttributes(typeof(DefaultAttribute)).First()).Value;

                    if (value is string)
                    {
                        string strValue = (string)value;
                        if (strValue.StartsWith("path:"))
                            value = Environment.ExpandEnvironmentVariables(strValue.Replace("path:", ""));
                    }

                    property.SetValue(null, value);
                }
            }
        }
        #endregion

        #region properties
        /// <summary>
        /// The directory where the keyring is located. If no keyring is in place, a new keyring will be created. This should be changed during startup. Default is %appdata%/storj/key.ring
        /// </summary>
        [Default("path:%appdata%\\storj\\key.ring")]
        public static string KeyRingFile { get; set; }

        /// <summary>
        /// The passphrase to decrypt the keyring
        /// </summary>
        public static string KeyRingPassphrase { get; set; }

        /// <summary>
        /// Where the shards of the file that is currently being uploaded shall be stored.
        /// </summary>
        [Default("path:%appdata%\\storj\\temp")]
        public static string ShardDirectory { get; set; }

        /// <summary>
        /// The size of the file shards. Smaller means more shards but is more storage efficient. Larger means less farmers involved.
        /// </summary>
        [Default(1024 * 1024)]
        public static int ShardSize { get; set; }

        /// <summary>
        /// The number of challenges to be generated per shard.
        /// </summary>
        [Default(8)]
        public static int ChallengesPerShard { get; set; }
        #endregion

        /// <summary>
        /// Authenticates a user given his email and password
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="password">The user's password (plain text)</param>
        public static void AuthenticateBasic(string email, string password)
        {
            StorjRestClient.AuthenticateBasic(email, HashUtil.SHA256EncryptToHex(password));
            TestAuthentication();
        }

        /// <summary>
        /// Authenticates a user given his private and public key
        /// </summary>
        /// <param name="privatekey">The user's private key, of which the matching public key has to be associated with the user account.</param>
        /// <param name="publickey">The matching public key that has to be associated with the user account.</param>
        public static void Authenticate(string privatekey, string publickey)
        {
            StorjRestClient.Authenticate(privatekey, publickey);
            TestAuthentication();
        }

        private static void TestAuthentication()
        {
            ListKeys();
        }

        /// <summary>
        /// Creates a new bucket with the given name. Important: User needs to authenticate first.
        /// </summary>
        /// <param name="bucketname">The name of the bucket to be created.</param>
        /// <returns>An object of the created bucket.</returns>
        public static Bucket CreateBucket(string bucketname)
        {
            StorjRestResponse<Bucket> response;

            //using the current key if authenticated using private key
            //disadvantage: only current key has access
            //advantage: significantly faster than retrieving all keys first
            if (StorjRestClient.AuthenticationMethod == AuthenticationMethod.SIGNATURE)
                 response = StorjRestClient.Request<Bucket>(new CreateBucketRequest("bucketname", (new string[1] { StorjRestClient.GetPublicKey() }).ToList()));
            else
            {
                //if logged in by basic authentication: retrieve list of keys first
                List<ECDSAKey> keys = ListKeys();

                List<string> keyList = new List<string>();
                foreach (ECDSAKey key in keys)
                    keyList.Add(key.PublicKey);

                response = StorjRestClient.Request<Bucket>(new CreateBucketRequest("bucketname", keyList));
            }

            if ((int)response.StatusCode != 201)
                ThrowStorjResponseError(new BucketCreationException(), response.Response);

            return response.ToObject();
        }

        /// <summary>
        /// Creates a new user with the email and password, generates a keypair and returns the resulting private key. If another user is authenticated, he will be logged out.
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="password">The user's password</param>
        /// <returns>The generated keyset (private, public)</returns> 
        public static Tuple<string, string> CreateUser(string email, string password)
        {
            StorjRestClient.Logout();

            //generate tuple
            Tuple<string, string> keyPair = GenerateKeyPair();

            //convert tuple to public key formatted for bridge
            string publicKey = ECDsaUtil.ExportPublicKeyQ(ECDsaUtil.ImportKey(keyPair.Item1, keyPair.Item2).Public);

            StorjRestResponse<object> response = StorjRestClient.Request(new CreateUserRequest(email, HashUtil.SHA256EncryptToHex(password), publicKey));

            if ((int)response.StatusCode != 201)
                ThrowStorjResponseError(new StorjException(), response.Response);

            return keyPair;
        }

        /// <summary>
        /// Destroys the bucket by its Id.
        /// </summary>
        /// <param name="bucketId">The Bucket's unique id.</param>
        public static void DestroyBucket(string bucketId)
        {
            StorjRestResponse<object> response = StorjRestClient.Request(new DeleteBucketRequest(bucketId));

            if ((int)response.StatusCode != 204)
                ThrowStorjResponseError(new DeleteBucketException(), response.Response);
        }

        /// <summary>
        /// Destroys a file by its Id.
        /// </summary>
        /// <param name="bucketId">The Id of the bucket where the file is stored in.</param>
        /// <param name="fileId">The Id of the file to destroy.</param>
        public static void DestroyFile(string bucketId, string fileId)
        {
            StorjRestResponse<object> response = StorjRestClient.Request(new DeleteFileRequest(bucketId, fileId));

            if ((int)response.StatusCode != 204)
                ThrowStorjResponseError(new DeleteFileException(), response.Response);
        }

        /// <summary>
        /// Downloads the file (fileId) from the bucket with the specified bucketId to the specified filepath. If the cipher is null, it will be loaded from the keyring.
        /// The method runs synchronously and terminates after the download is complete or otherwise fails with an exception.
        /// </summary>
        /// <param name="bucketId">The Id of the bucket where the file is stored in.</param>
        /// <param name="fileId">The Id of the file to be downloaded.</param>
        /// <param name="filepath">The path where the file shall be downloaded to.</param>
        /// <param name="downloadProgressEvent">A method with one parameter of type DownloadProgressEventArgs that is triggered when there is progress to report.</param>
        /// <param name="cipher">A cipher to override the cipher automatically loaded from the key ring.</param>
        public static void DownloadFile(string bucketId, string fileId, string filepath, Action<DownloadProgressEventArgs> downloadProgressEvent, Cipher cipher = null)
        {
            new FileDownloader(bucketId, fileId, filepath, downloadProgressEvent, cipher).Start();
        }

        /// <summary>
        /// Retrieves the first bucket in the user's buckets that matches a given name.
        /// </summary>
        /// <param name="bucketname">The bucket name to search for.</param>
        /// <returns>The bucket object matching the given name.</returns>
        public static Bucket FindBucket(string bucketname)
        {
            List<Bucket> buckets = ListBuckets();
            foreach (Bucket bucket in buckets)
            {
                if (bucket.Name.Equals(bucketname))
                    return bucket;
            }

            return null;
        }

        /// <summary>
        /// Randomly creates a new cipher object.
        /// </summary>
        /// <returns>A randomly created cipher.</returns>
        public static Cipher GenerateCipher()
        {
            return CryptoUtil.GenerateCipher();
        }

        /// <summary>
        /// Generates a keypair that is being registered with the current user. Important: User needs to authenticate first.
        /// </summary>
        /// <returns>A tuple of the private and the public key. (In that order)</returns>
        public static Tuple<string, string> GenerateKeyPair()
        {
            AsymmetricCipherKeyPair keys = ECDsaUtil.GenerateKeyPair();

            string privateKey = ECDsaUtil.ExportPrivateKey(keys.Private);
            string publicKey = ECDsaUtil.ExportPublicKey(keys.Public);

            if (StorjRestClient.AuthenticationMethod == AuthenticationMethod.NOT_SET)
                return new Tuple<string, string>(privateKey, publicKey);

            StorjRestResponse<object> response = StorjRestClient.Request(new RegisterKeyRequest(ECDsaUtil.ExportPublicKeyQ(keys.Public)));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                ThrowStorjResponseError(new KeyRegistrationException(), response.Response);

            return new Tuple<string, string>(privateKey, publicKey);
        }

        /// <summary>
        /// Retrieves the current user's buckets. Important: User needs to authenticate first.
        /// </summary>
        /// <returns>A list of bucket objects or an empty list, if there are no buckets.</returns>
        public static List<Bucket> ListBuckets()
        {
            StorjRestResponse<List<Bucket>> response = StorjRestClient.Request<List<Bucket>>(new ListBucketsRequest());

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                ThrowStorjResponseError(new BucketListException(), response.Response);

            return response.ToObject();
        }

        /// <summary>
        /// Lists all files in a bucket.
        /// </summary>
        /// <param name="bucketId">The Id of the bucket.</param>
        /// <returns>A List of all the Files in the bucket or an empty list, if the bucket is empty.</returns>
        public static List<StorjFile> ListFiles(string bucketId)
        {
            StorjRestResponse<List<StorjFile>> response = StorjRestClient.Request<List<StorjFile>>(new GetFilesInBucketRequest(bucketId));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                ThrowStorjResponseError(new BucketFileListException(), response.Response);

            return response.ToObject();
        }

        /// <summary>
        /// Lists all public keys registered with the current user account.
        /// </summary>
        /// <returns>A list of ECDSAKey objects representing the user's public keys.</returns>
        public static List<ECDSAKey> ListKeys()
        {
            StorjRestResponse<List<ECDSAKey>> response = StorjRestClient.Request<List<ECDSAKey>>(new ListKeysRequest());

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                ThrowStorjResponseError(new KeyListException(), response.Response);

            return response.ToObject();
        }

        /// <summary>
        /// Wipes all user data from the cache and logs the user out.
        /// </summary>
        public static void Logout()
        {
            StorjClient.Logout();
        }

        /// <summary>
        /// Encrypts and uploads a file (filepath) into the bucket (bucketId) and triggers the uploadProgressEvent method when there is progress to report
        /// </summary>
        /// <param name="bucketId">The Id of the bucket where the file shall be uploaded.</param>
        /// <param name="filepath">The path of the file to upload.</param>
        /// <param name="uploadProgressEvent">A method with one parameter of type UploadProgressEventArgs that is triggered when there is progress to report.</param>
        /// <param name="storjFileName">The name of the file in the storj network.</param>
        /// <param name="cipher">A cipher that overrides the automatically generated cipher. Use only, when files shall always be encrypted with the same key.
        /// Only automatically generated ciphers will be saved in the keyring.</param>
        /// <returns>The StorjFile object representing the file in the Storj network.</returns>
        public static StorjFile UploadFile(string bucketId, string filepath, Action<UploadProgressEventArgs> uploadProgressEvent, string storjFileName = "", Cipher cipher = null)
        {
            FileUploader uploader = new FileUploader(bucketId, filepath, uploadProgressEvent, storjFileName, cipher);
            return uploader.Start();
        }

        internal static void ThrowStorjResponseError(StorjException exception, IRestResponse response)
        {
            exception.Message = "REST returned an error with code " + response.StatusCode.ToString() + " message: " + response.Content;
            throw exception;
        }
    }
}

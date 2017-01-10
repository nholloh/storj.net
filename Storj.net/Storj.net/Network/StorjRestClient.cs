using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using RestSharp;
using RestSharp.Authenticators;
using Storj.net.Model;
using Storj.net.Model.Attribute;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    class StorjRestClient
    {
        const string API_ROOT = "https://api.storj.io";

        [Default(AuthenticationMethod.NOT_SET)]
        public static AuthenticationMethod AuthenticationMethod { get; set; }

        static RestClient restClient = new RestClient(API_ROOT);

        static string username = "";
        static string password = "";

        public static AsymmetricCipherKeyPair Keys { get; set; }
        static string publicKey = "";

        #region authentication
        public static void AuthenticateBasic(string username, string password)
        {
            AuthenticationMethod = AuthenticationMethod.CREDENTIALS;

            StorjRestClient.username = username;
            StorjRestClient.password = password;

            StorjRestClient.Keys = null;
            StorjRestClient.publicKey = "";
        }

        public static void Authenticate(string privatekey, string publickey)
        {
            AuthenticationMethod = AuthenticationMethod.SIGNATURE;

            StorjRestClient.Keys = ECDsaUtil.ImportKey(privatekey, publickey);
            StorjRestClient.publicKey = ECDsaUtil.ExportPublicKeyQ(Keys.Public);

            StorjRestClient.username = "";
            StorjRestClient.password = "";
        }

        public static void Logout()
        {
            AuthenticationMethod = AuthenticationMethod.NOT_SET;

            StorjRestClient.Keys = null;
            StorjRestClient.publicKey = "";
            StorjRestClient.username = "";
            StorjRestClient.password = "";
        }
        #endregion

        public static StorjRestResponse<object> Request(StorjRestRequest requestData)
        {
            return Request<object>(requestData);
        }

        public static StorjRestResponse<T> Request<T>(StorjRestRequest requestData)
        {
            requestData.ResolveEndpointVars();
            requestData.ResolveHeaderVars();

            RestRequest request = new RestRequest(requestData.Endpoint, requestData.Method);

            //prepare data
            string requestDataString = requestData.ToDataString();

            // prepare header
            if (AuthenticationMethod == AuthenticationMethod.CREDENTIALS)
                new HttpBasicAuthenticator(username, password).Authenticate(restClient, request);
            else if (AuthenticationMethod == AuthenticationMethod.SIGNATURE)
            {
                request.AddHeader("x-pubkey", publicKey);
                request.AddHeader("x-signature", Sign(requestData.Method.ToString() + "\n" + requestData.Endpoint + "\n" + requestDataString));
            }

            foreach (KeyValuePair<string, string> headerAttribute in requestData.HeaderAttributes)
                request.AddHeader(headerAttribute.Key, headerAttribute.Value);

            if (requestData.IsQueryOnly())
                request.AddQueryParameter("__nonce", requestData.Nonce);
            else
                request.AddParameter("application/json", requestDataString, ParameterType.RequestBody);

            IRestResponse response = restClient.Execute(request);

            return new StorjRestResponse<T>(response);
        }

        public static string GetPublicKey()
        {
            return ECDsaUtil.ExportPublicKeyQ(Keys.Public);
        }

        public static string Sign(string data)
        {
            return ECDsaUtil.Sign(data, Keys.Private);
        }
    }

    enum AuthenticationMethod
    {
        NOT_SET,
        CREDENTIALS,
        SIGNATURE
    }
}

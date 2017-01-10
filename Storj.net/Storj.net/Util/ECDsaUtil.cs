using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Storj.net.Model;
using Storj.net.Model.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net.Util
{
    class ECDsaUtil
    {
        const string ECDSA_CURVE = "secp256k1";

        internal static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            // code from: Kevin Baird (Github: tempestb)
            // https://github.com/tempestb/StorjECDSAFinal/blob/master/ConsoleApplication1/ConsoleApplication1/Program.cs

            var gen = new ECKeyPairGenerator();
            var secureRandom = new SecureRandom();
            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName(ECDSA_CURVE);
            var ecParams = new ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);
            var keyGenParam = new ECKeyGenerationParameters(ecParams, secureRandom);
            gen.Init(keyGenParam);
            return gen.GenerateKeyPair();
        }

        internal static AsymmetricCipherKeyPair ImportKey(string privateKey, string publicKey)
        {
            // code from: Kevin Baird (Github: tempestb)
            // source: chat on community.storj.io

            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            var ecSpec = new ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);

            ECPrivateKeyParameters privatekey = (ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
            ECPublicKeyParameters publickey = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));

            return new AsymmetricCipherKeyPair(publickey, privatekey);
        }

        internal static string ExportPrivateKey(AsymmetricKeyParameter privateKey)
        { 
            PrivateKeyInfo pk = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);
            return Convert.ToBase64String(pk.ToAsn1Object().GetDerEncoded());
        }

        internal static string ExportPublicKey(AsymmetricKeyParameter publicKey)
        {
            SubjectPublicKeyInfo pk = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
            return Convert.ToBase64String(pk.ToAsn1Object().GetDerEncoded());
        }

        internal static string ExportPublicKeyQ(AsymmetricKeyParameter publicKey)
        {
            return HashUtil.HexEncodeToString(((ECPublicKeyParameters)publicKey).Q.GetEncoded());
        }

        internal static string Sign(string data, AsymmetricKeyParameter privateKey)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(data);

            ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, privateKey);
            signer.BlockUpdate(msgBytes, 0, msgBytes.Length);

            return HashUtil.HexEncodeToString(signer.GenerateSignature());
        }
    }
}
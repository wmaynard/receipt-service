using System;
using System.Text;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
// ReSharper disable ArrangeTypeMemberModifiers

namespace Rumble.Platform.ReceiptService.Utilities
{
    class GoogleSignatureVerify
    {
        RSAParameters _rsaKeyInfo;

        public GoogleSignatureVerify(string googlePublicKey)
        {
            RsaKeyParameters rsaParameters= (RsaKeyParameters) PublicKeyFactory.CreateKey(Convert.FromBase64String(googlePublicKey)); 

            byte[] rsaExp   = rsaParameters.Exponent.ToByteArray();
            byte[] modulus  = rsaParameters.Modulus.ToByteArray();

            // Microsoft RSAParameters modulo wants leading zero's removed so create new array with leading zero's removed
            int pos = 0;
            for (int i = 0; i < modulus.Length; i++)
            {
                if (modulus[i] == 0) 
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            byte[] rsaMod = new byte[modulus.Length - pos];
            Array.Copy(modulus,pos,rsaMod,0,modulus.Length - pos);

            // Fill the Microsoft parameters
            _rsaKeyInfo = new RSAParameters()
                          {
                              Exponent    = rsaExp,
                              Modulus     = rsaMod
                          };
        }

        public bool Verify(string message, string signature)
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(_rsaKeyInfo);  
            return rsa.VerifyData(Encoding.ASCII.GetBytes(message), "SHA1", Convert.FromBase64String(signature));
        }
    }
}
using System;
using System.Text;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.ReceiptService.Models;

// ReSharper disable ArrangeTypeMemberModifiers

namespace Rumble.Platform.ReceiptService.Utilities
{
    public static class GoogleValidator
    {
        private static RSAParameters _rsaKeyInfo = Initialize();
        private static bool _initialized;
        
        // TODO: Figure out how many validations succeed / fail the first method.
        // The original code would log a warning when falling back to the second, but even from local testing it seemed
        // to be the more common validation.  We ended up having quite a few warnings that were not actionable.
        public static bool IsValid(RumbleJson rawReceipt, string signature)
        {
            try
            {
                string message = rawReceipt.ToJson();
                using RSACryptoServiceProvider rsa = new();
                rsa.ImportParameters(_rsaKeyInfo);
                return rsa.VerifyData(Encoding.ASCII.GetBytes(message), "SHA1", Convert.FromBase64String(signature));
            }
            catch (Exception e)
            {
                Log.Critical(Owner.Will, "Error occured while attempting to validate Google receipt.", data: new
                {
                    RawData = rawReceipt,
                    Signature = signature
                }, exception: e);
                return false;
            }
        }

        private static RSAParameters Initialize()
        {
            if (_initialized)
                return _rsaKeyInfo;

            string aosKey = PlatformEnvironment.Require<string>("androidStoreKey");
            RsaKeyParameters rsaParameters = (RsaKeyParameters) PublicKeyFactory.CreateKey(Convert.FromBase64String(aosKey)); 

            byte[] modulus = rsaParameters.Modulus.ToByteArray();

            // Microsoft RSAParameters modulo wants leading zero's removed so create new array with leading zero's removed
            int pos = 0;
            while (pos < modulus.Length && modulus[pos] == 0)
                pos++;
            
            byte[] rsaMod = new byte[modulus.Length - pos];
            Array.Copy(
                sourceArray: modulus,
                sourceIndex: pos,
                destinationArray: rsaMod,
                destinationIndex: 0,
                length: modulus.Length - pos
            );

            // Fill the Microsoft parameters
            _rsaKeyInfo = new RSAParameters
            {
                Exponent = rsaParameters.Exponent.ToByteArray(),
                Modulus = rsaMod
            };
            _initialized = true;
            return _rsaKeyInfo;
        }
    }
}
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Rumble.Platform.ReceiptService.Services
{
    public class GoogleService : VerificationService
    {
        // google specific looks at receipt, signature, channel?, game
        public GoogleValidation VerifyGoogle(Receipt receipt, string accountId = null, string signature = null)
        {
            GoogleValidation verification = null;
            string transactionId = receipt.OrderId;
            string offerId = receipt.ProductId;

            if (transactionId == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No orderId. Receipt {receipt}");
                return null;
            }

            if (offerId == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No product ID. Receipt {receipt}");
                return null;
            }

            if (signature == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No signature. Receipt {receipt}");
                return null;
            }
            
            // TODO
            // androidStoreKey is a RSA public key. private key not provided by google
            // maybe eventually make actual certificates to store

            // perhaps purchaseInfoBytes is off? format is correct according to google specs, but extra acknowledged and id fields
            Object purchaseInfo;
            purchaseInfo.orderId = 
            byte[] purchaseInfoBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(receipt));
            
            byte[] sigBytes = Convert.FromBase64String(signature);
            
            byte[] keyBytes = Convert.FromBase64String(PlatformEnvironment.Variable(name: "androidStoreKey"));
            // AsnEncodedData asnKey = new AsnEncodedData(byteKey);
            // X509Certificate2 cert = new X509Certificate2(byteKey);

            AsymmetricKeyParameter asymmetricKeyParameter = PublicKeyFactory.CreateKey(keyBytes);
            RsaKeyParameters rsaKeyParameters = (RsaKeyParameters) asymmetricKeyParameter;
            RSAParameters rsaParameters = new RSAParameters();
            rsaParameters.Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned();
            rsaParameters.Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned();
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaParameters);

            SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(purchaseInfoBytes);
            
            RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm("SHA1withRSA");

            bool verified = false;
            try
            {
                // the following appears to return false for the provided sample data sent
                verified = rsaDeformatter.VerifySignature(hash, sigBytes);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: $"Error occured while attempting to very Google receipt signature. Receipt {receipt}");
                return null;
            }
            
            if (verified)
            {
                string receiptKey = $"{PlatformEnvironment.Variable(name: "RUMBLE_DEPLOYMENT")}_s_aosReceipt_{transactionId}";
                
                verification = new GoogleValidation(
                    status: "success",
                    response: receipt,
                    transactionId: transactionId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.ToString(),
                    timestamp: receipt.PurchaseTime
                );
            }
            
            return verification;
        }
    }
}
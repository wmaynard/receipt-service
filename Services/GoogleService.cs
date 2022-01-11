using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services
{
    public class GoogleService : VerificationService
    {
        // google specific looks at receipt, signature
        public VerificationResult VerifyGoogle(Receipt receipt, string signature = null)
        {
            VerificationResult verification = null;
            string transactionId = receipt.OrderId;
            string offerId = receipt.ProductId;

            if (transactionId == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No orderId. Receipt {receipt.JSON}");
                return null;
            }

            if (offerId == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No product ID. Receipt {receipt.JSON}");
                return null;
            }

            if (signature == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to verify Google receipt. No signature. Receipt {receipt.JSON}");
                return null;
            }
            
            // TODO
            // need a valid receipt to test, not sure if the one on documentation is outdated
            // androidStoreKey is a RSA public key. private key not provided by google
            // maybe eventually make actual certificates to store
            
            // need to match exactly for verification, use googlevalidation for this purpose

            GoogleValidation purchaseInfo = new GoogleValidation(orderId: receipt.OrderId, packageName: receipt.PackageName, productId: receipt.ProductId, purchaseTime: receipt.PurchaseTime, purchaseState: receipt.PurchaseState, purchaseToken: receipt.PurchaseToken, acknowledged: receipt.Acknowledged);
            
            byte[] purchaseInfoBytes = Encoding.UTF8.GetBytes(purchaseInfo.JSON);
            // byte[] purchaseInfoBytes = Convert.FromBase64String(Convert.ToBase64String(Encoding.UTF8.GetBytes(purchaseInfo.JSON)));
            
            // byte[] sigBytes = Encoding.UTF8.GetBytes(signature);
            byte[] sigBytes = Convert.FromBase64String(signature);
            
            // byte[] keyBytes = Encoding.UTF8.GetBytes(PlatformEnvironment.Variable(name: "androidStoreKey"));
            byte[] keyBytes = Convert.FromBase64String(PlatformEnvironment.Variable(name: "androidStoreKey"));
            
            // AsymmetricKeyParameter asymmetricKeyParameter = PublicKeyFactory.CreateKey(keyBytes);
            // RsaKeyParameters rsaKeyParameters = (RsaKeyParameters) asymmetricKeyParameter;
            
            RSAParameters rsaParameters = new RSAParameters();

            // rsaParameters.Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned();
            // rsaParameters.Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned();

            byte[] modulus = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                modulus[i] = keyBytes[33 + i];
            }

            byte[] exponent = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                exponent[i] = keyBytes[291 + i];
            }

            rsaParameters.Modulus = modulus;
            rsaParameters.Exponent = exponent;
            
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            rsa.ImportParameters(rsaParameters);

            SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(purchaseInfoBytes);
            
            RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm(strName: "SHA1");
            bool verified = false;
            try
            {
                // TODO try another receipt with a tested valid signature
                // the following appears to return false for the provided sample data sent
                verified = rsaDeformatter.VerifySignature(rgbHash: hash, rgbSignature: sigBytes);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: $"Error occured while attempting to verify Google receipt signature. {e.Message}. Receipt {receipt.JSON}");
                return null;
            }

            if (verified)
            {
                string receiptKey = $"{PlatformEnvironment.Variable(name: "RUMBLE_DEPLOYMENT")}_s_aosReceipt_{transactionId}";
                
                verification = new VerificationResult(
                    status: "success",
                    response: receipt,
                    transactionId: transactionId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
            }
            else
            {
                verification = new VerificationResult(
                    status: "failed",
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: null,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
                Log.Error(owner: Owner.Nathan, message: $"Failure to validate Google receipt. Receipt: {receipt.JSON}");
            }
            
            return verification;
        }
    }
}
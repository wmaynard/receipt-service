using System;
using RCL.Logging;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Services;
    
public class GoogleService : VerificationService
{
    // google specific looks at receipt, signature
    public static VerificationResult VerifyGoogle(Receipt receipt, string signature = null)
    {
        VerificationResult verification = null;
        
        if (signature == null)
        {
            throw new ReceiptException(receipt, "Failed to verify Google receipt. No signature provided.");
        }

        bool verified;

        try
        {
            // androidStoreKey is a RSA public key. private key not provided by google
            GoogleSignatureVerify signatureVerify =
                new GoogleSignatureVerify(PlatformEnvironment.Require(key: "androidStoreKey"));
            verified = signatureVerify.Verify(receipt.JSON.Replace(",\"accountId\":null,\"id\":null", ""), signature);
        }
        catch (Exception e)
        {
            Log.Error(owner: Owner.Nathan, message: "Error occured while attempting to verify Google receipt signature.", data: $"{e.Message}. Receipt: {receipt.JSON}");
            return null;
        }

        // if (true) // testing only, remove when rsa fixed
        if (verified)
        {
            string receiptKey = $"{PlatformEnvironment.Deployment}_s_aosReceipt_{receipt.OrderId}";
            
            verification = new VerificationResult(
                status: "success",
                response: receipt,
                transactionId: receipt.OrderId,
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
            Log.Error(owner: Owner.Nathan, message: "Failure to validate Google receipt.", data: $"Receipt: {receipt.JSON}");
        }
        
        return verification;
    }
}
using System;
using System.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Services;
    
public class GoogleService : VerificationService
{
#pragma warning disable
    private readonly ReceiptService _receiptService;
#pragma warning restore

    // Attempts to verify an aos receipt
    public VerificationResult VerifyGoogle(Receipt receipt, string signature, RumbleJson receiptData, string accountId, bool forceValidation)
    {
        if (forceValidation)
        {
            string receiptKey = $"{PlatformEnvironment.Deployment}_s_aosReceipt_{receipt.OrderId}";
            Receipt storedReceipt = _receiptService
                                    .Find(filter: existingReceipt => existingReceipt.OrderId == receipt.OrderId)
                                    .FirstOrDefault();
            
            if (storedReceipt == null)
            {
                return new VerificationResult(
                                              status: VerificationResult.SuccessStatus.True,
                                              response: receipt,
                                              transactionId: receipt.OrderId,
                                              offerId: receipt.ProductId,
                                              receiptKey: receiptKey,
                                              receiptData: receipt.JSON,
                                              timestamp: receipt.PurchaseTime
                                             );
            }
            
            if (storedReceipt.AccountId == accountId)
            {
                return new VerificationResult(
                                              status: VerificationResult.SuccessStatus.Duplicated,
                                              response: receipt,
                                              transactionId: receipt.OrderId,
                                              offerId: receipt.ProductId,
                                              receiptKey: receiptKey,
                                              receiptData: receipt.JSON,
                                              timestamp: receipt.PurchaseTime
                                             );
            }
        }
        
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
            if (verified == false)
            {
                Log.Warn(owner: Owner.Nathan,
                         message: "Verifying Google receipt failed. Falling back to verifying raw data...", data: $"Account ID: {accountId}");
                verified = signatureVerify.Verify(receiptData.Json, signature);
                if (verified)
                {
                    Log.Warn(owner: Owner.Nathan,
                             message:
                             "Verifying Google receipt failed using the Receipt model, but succeeded when falling back to raw data. Google's receipt data may have changed.", data: $"Account ID: {accountId}");
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(owner: Owner.Nathan, message: "Error occured while attempting to verify Google receipt.",
                      data:$"Account ID: {accountId}. Exception: {e.Message}. Receipt data: {receiptData.Json}");
            return null;
        }

        if (verified)
        {
            string receiptKey = $"{PlatformEnvironment.Deployment}_s_aosReceipt_{receipt.OrderId}";
            Receipt storedReceipt = _receiptService
                                    .Find(filter: existingReceipt => existingReceipt.OrderId == receipt.OrderId)
                                    .FirstOrDefault();
            
            if (storedReceipt == null)
            {
                return new VerificationResult(
                    status: VerificationResult.SuccessStatus.True,
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
            }
            
            if (storedReceipt.AccountId == accountId)
            {
                return new VerificationResult(
                    status: VerificationResult.SuccessStatus.Duplicated,
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
            }
            
            if (storedReceipt.AccountId != accountId)
            {
                Log.Warn(owner: Owner.Nathan, message: "Duplicated receipt processed but account IDs did not match.", data: $"Request account ID: {accountId}. Receipt: {receipt}");
                return new VerificationResult(
                    status: VerificationResult.SuccessStatus.DuplicatedFail,
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
            }
        }
        Log.Error(owner: Owner.Nathan, message: "Failure to validate Google receipt.", data: $"Account ID: {accountId}. Receipt data: {receiptData.Json}. Signature: {signature}");
        return new VerificationResult(
            status: VerificationResult.SuccessStatus.False,
            response: receipt,
            transactionId: receipt.OrderId,
            offerId: receipt.ProductId,
            receiptKey: null,
            receiptData: receipt.JSON,
            timestamp: receipt.PurchaseTime
        );
    }
}
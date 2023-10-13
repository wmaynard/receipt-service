using System;
using System.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Services;
    
public class GoogleService : VerificationService
{
#pragma warning disable
    private readonly ReceiptService          _receiptService;
    private readonly ForcedValidationService _forcedValidationService;
    private readonly ApiService              _apiService;
#pragma warning restore

    // Attempts to verify an aos receipt
    public VerificationResult VerifyGoogle(Receipt receipt, string signature, RumbleJson receiptData, string accountId)
    {
        bool forceValidation = _forcedValidationService.CheckTransactionId(receipt.OrderId);
        
        if (forceValidation)
            return new VerificationResult
            {
                Status = _receiptService.Exists(orderId: receipt.OrderId)
                    ? VerificationResult.SuccessStatus.True
                    : VerificationResult.SuccessStatus.Duplicated,
                Response = receipt,
                TransactionId = receipt.OrderId,
                OfferId = receipt.ProductId,
                ReceiptKey = $"{PlatformEnvironment.Deployment}_s_aosReceipt_{receipt.OrderId}",
                ReceiptData = receipt.JSON,
                Timestamp = receipt.PurchaseTime
            };
        
        if (signature == null)
            throw new ReceiptException(receipt, "Failed to verify Google receipt. No signature provided.");

        try
        {
            // androidStoreKey is a RSA public key. private key not provided by google
            GoogleSignatureVerify signatureVerify = new();
            bool verified = signatureVerify.Verify(receipt.JSON.Replace(",\"accountId\":null,\"id\":null", ""), signature);
            if (!verified)
            {
                Log.Warn(Owner.Will, "Verifying Google receipt failed. Falling back to verifying raw data...", data: new
                {
                    AccountId = accountId
                });
                verified = signatureVerify.Verify(receiptData.Json, signature);
                if (verified)
                    Log.Warn(Owner.Will, "Verifying Google receipt failed using the Receipt model, but succeeded when falling back to raw data. Google's receipt data may have changed.", data: new
                        {
                            AccountId = accountId
                        });
            }

            if (!verified)
                throw new PlatformException("Google receipt failed two different types of validation.");
            
            Receipt storedReceipt = _receiptService
                .Find(filter: existingReceipt => existingReceipt.OrderId == receipt.OrderId)
                .FirstOrDefault();

            return new VerificationResult
            {
                Status = storedReceipt switch
                {
                    null => VerificationResult.SuccessStatus.True,
                    _ when storedReceipt.AccountId == accountId => VerificationResult.SuccessStatus.Duplicated,
                    _ => VerificationResult.SuccessStatus.DuplicatedFail
                },
                Response = receipt,
                TransactionId = receipt.OrderId,
                OfferId = receipt.ProductId,
                ReceiptKey = $"{PlatformEnvironment.Deployment}_s_aosReceipt_{receipt.OrderId}",
                ReceiptData = receipt.JSON,
                Timestamp = receipt.PurchaseTime
            };
        }
        catch (Exception e)
        {
            _apiService.Alert(
                title: "Error occured while attempting to verify Google receipt.",
                message: "Error occured while attempting to verify Google receipt.",
                countRequired: 1,
                timeframe: 300,
                data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Exception", e },
                        { "Receipt Data", receiptData }
                    } 
            );
            
            return new VerificationResult(
                status: VerificationResult.SuccessStatus.False,
                response: receipt,
                transactionId: receipt.OrderId,
                offerId: receipt.ProductId,
                receiptKey: null,
                receiptData: receipt.JSON,
                timestamp: receipt.PurchaseTime
            );;
        }
    }
}
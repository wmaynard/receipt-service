using System;
using System.Linq;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Utilities;

// ReSharper disable InconsistentNaming

namespace Rumble.Platform.ReceiptService.Services;


public class VerificationService : PlatformService
{
    private readonly ReceiptService          _receiptService;
    private readonly ForcedValidationService _forcedValidationService;
    private readonly ApiService              _apiService;
    private readonly DynamicConfig _dynamicConfig;

    public VerificationService(ReceiptService receipt, ForcedValidationService validation, ApiService api, DynamicConfig config)
    {
        _receiptService = receipt;
        _forcedValidationService = validation;
        _apiService = api;
        _dynamicConfig = config;
    }

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

    // apple specific looks at receipt
    // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
    // requires password
    // requires exclude-old-transactions if auto-renewable subscriptions
    // assuming no subscriptions for now, possible to put in later if needed
    public AppleVerificationResult VerifyApple(string receipt, string transactionId, string accountId)
    {
        AppleValidation verified = VerifyAppleData(receipt, accountId);

        bool forceValidation = _forcedValidationService.CheckTransactionId(transactionId);
        
        if (forceValidation)
            return new AppleVerificationResult
            {
                Status = _receiptService.Exists(orderId: transactionId)
                    ? AppleVerificationResult.SuccessStatus.True
                    : AppleVerificationResult.SuccessStatus.Duplicated,
                Response = verified.Receipt,
                TransactionId = transactionId,
                ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{transactionId}",
                ReceiptData = verified.Receipt.JSON,
                Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
            };

        switch (verified.Status)
        {
            case 0 when verified.Receipt.InApp.Find(appleInApp => appleInApp.TransactionId == transactionId) == null:
                _apiService.Alert(
                    title: "Failure to validate Apple receipt.",
                    message: "Receipt validated correctly with Apple but no matching transaction ID was found.",
                    countRequired: 5,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Transaction ID", transactionId},
                        { "Receipt", receipt}
                    } 
                );
                    
                return new AppleVerificationResult
                {
                    Status = AppleVerificationResult.SuccessStatus.False,
                    Response = verified.Receipt,
                    TransactionId = transactionId,
                    ReceiptKey = null,
                    ReceiptData = verified.Receipt.JSON,
                    Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                };
            case 0:
                Receipt storedReceipt = _receiptService
                    .Find(filter: existingReceipt => existingReceipt.OrderId == transactionId)
                    .FirstOrDefault();
                
                return new AppleVerificationResult
                {
                    Status = storedReceipt switch
                    {
                        null => AppleVerificationResult.SuccessStatus.True,
                        _ when storedReceipt.AccountId == accountId => AppleVerificationResult.SuccessStatus.Duplicated,
                        _ => AppleVerificationResult.SuccessStatus.DuplicatedFail
                    },
                    Response = verified.Receipt,
                    TransactionId = transactionId,
                    ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{transactionId}",
                    ReceiptData = verified.Receipt.JSON,
                    Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                };
            // failed to authenticate or testflight on prod. Apple returns nothing but the status
            case 21003:
            case 21007:
            case 500:
            default:
                return new AppleVerificationResult
                {
                    Status = verified.Status.Between(21003, 21007)
                        ? AppleVerificationResult.SuccessStatus.False
                        : AppleVerificationResult.SuccessStatus.StoreOutage,
                    Response = null,
                    TransactionId = transactionId,
                    ReceiptKey = null,
                    ReceiptData = null,
                    Timestamp = 0
                };
        }
    }
    
    public AppleValidation VerifyAppleData(string receipt, string accountId) // apple takes stringified version of receipt, includes receipt-data, password
    {
        bool isProd = PlatformEnvironment.IsProd || _dynamicConfig.Require<bool>("isProd");

        _apiService
            .Request(isProd
                ? _dynamicConfig.Require<string>("iosVerifyReceiptUrl")
                : _dynamicConfig.Require<string>("iosVerifyReceiptSandbox")
            )
            .SetPayload(new RumbleJson
            {
                {"receipt-data", receipt},
                {"password", PlatformEnvironment.Require<string>("appleSharedSecret")}
            })
            .OnSuccess(res => { })
            .OnFailure(res =>
            {
                _apiService.Alert(
                    title: "Exception when sending a request to Apple.  App Store may be down.",
                    message: "An exception was encountered when sending a request to Apple's App store.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId},
                        { "Response", res }
                    },
                    owner: Owner.Will
                );
            })
            .Post(out AppleValidation validation, out int code);

        switch (validation.Status)
        {
            // 21007: This receipt is from the test environment, but you sent it to the production environment for verification.
            case 21007 when isProd:
                Log.Warn(Owner.Will, message: "A testflight purchase was attempted on the production environment. This receipt validation is thus blocked.", data: new
                {
                    AccountId = accountId
                });
                return validation;
            case 21007:
            case 0:
                return validation;
            default:
                _apiService.Alert(
                    title: "Failed to validate iOS receipt.",
                    message: "Failed to validate iOS receipt. Apple's App store may have an outage.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Status", validation.Status }
                    },
                    owner: Owner.Will
                );
                return validation;
        }
    }
}

// response from apple
// string environment (Production, Sandbox)
// boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
// byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
// list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
// list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
// json receipt (json) of receipt sent for verification
// int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes
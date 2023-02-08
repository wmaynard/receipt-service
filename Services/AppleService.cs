using System;
using System.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Models;
// ReSharper disable MemberCanBePrivate.Global

namespace Rumble.Platform.ReceiptService.Services;

public class AppleService : VerificationService
{
#pragma warning disable
    private readonly ApiService     _apiService;
    private readonly ReceiptService _receiptService;
    private readonly DynamicConfig  _dynamicConfig;
#pragma warning restore

    public AppleService(ApiService apiService)
    {
        _apiService = apiService;
    }

    // apple specific looks at receipt
    // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
    // requires password
    // requires exclude-old-transactions if auto-renewable subscriptions
    // assuming no subscriptions for now, possible to put in later if needed
    public AppleVerificationResult VerifyApple(string receipt, string transactionId, string accountId)
    {
        AppleValidation verified = VerifyAppleData(receipt);

        if (verified.Status == 0)
        {
            AppleInApp inApp = verified.Receipt.InApp.Find(appleInApp => appleInApp.TransactionId == transactionId);
            if (inApp == null)
            {
                throw new PlatformException(message:
                                            "Receipt validated correctly with Apple but no matching transaction ID was found.");
            }

            Receipt storedReceipt = _receiptService
                                         .Find(filter: existingReceipt => existingReceipt.OrderId == transactionId)
                                         .FirstOrDefault();
            
            if (storedReceipt == null)
            {
                return new AppleVerificationResult
                       {
                           Status = AppleVerificationResult.SuccessStatus.True,
                           Response = verified.Receipt,
                           TransactionId = transactionId,
                           ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{transactionId}",
                           ReceiptData = verified.Receipt.JSON,
                           Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                       };
            }
            
            if (storedReceipt.AccountId == accountId)
            {
                return new AppleVerificationResult
                   {
                       Status = AppleVerificationResult.SuccessStatus.Duplicated,
                       Response = verified.Receipt,
                       TransactionId = transactionId,
                       ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{transactionId}",
                       ReceiptData = verified.Receipt.JSON,
                       Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                   };
            }

            if (storedReceipt.AccountId != accountId)
            {
                Log.Warn(owner: Owner.Nathan, message: "Duplicated receipt processed but account IDs did not match.", data: receipt);
                
                return new AppleVerificationResult
                   {
                       Status = AppleVerificationResult.SuccessStatus.DuplicatedFail,
                       Response = verified.Receipt,
                       TransactionId = transactionId,
                       ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{transactionId}",
                       ReceiptData = verified.Receipt.JSON,
                       Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                   };
            }
        }

        if (verified.Status == 21003)
        {
            return new AppleVerificationResult
               {
                   Status = AppleVerificationResult.SuccessStatus.False,
                   Response = verified?.Receipt,
                   TransactionId = transactionId,
                   ReceiptKey = null,
                   ReceiptData = verified?.Receipt.JSON,
                   Timestamp = Convert.ToInt64(verified?.Receipt.ReceiptCreationDateMs)
               };
        }

        if (verified.Status == 500)
        {
            return new AppleVerificationResult
               {
                   Status = AppleVerificationResult.SuccessStatus.StoreOutage,
                   Response = null,
                   TransactionId = transactionId,
                   ReceiptKey = null,
                   ReceiptData = null,
                   Timestamp = 0
               };
        }
        
        return new AppleVerificationResult
           {
               Status = AppleVerificationResult.SuccessStatus.StoreOutage,
               Response = verified?.Receipt,
               TransactionId = transactionId,
               ReceiptKey = null,
               ReceiptData = verified?.Receipt?.JSON,
               Timestamp = Convert.ToInt64(verified?.Receipt?.ReceiptCreationDateMs)
           };
    }

    // Sends the request to attempt to verify receipt data
    public AppleValidation VerifyAppleData(string receipt) // apple takes stringified version of receipt, includes receipt-data, password
    {
        string sharedSecret = PlatformEnvironment.Require(key: "appleSharedSecret"); // for some reason this is trying to get from request payload
        
        _apiService
            .Request(_dynamicConfig.Require<string>(key: "iosVerifyReceiptUrl"))
            .SetPayload(new RumbleJson
            {
                { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                { "password", sharedSecret }
            })
            .Post(out AppleValidation response, out int code);

        if (!code.Between(200, 299))
        {
            Log.Error(owner: Owner.Nathan, message: "Request to Apple's App Store failed. App store is down.", data:$"Code: {code}");

            AppleValidation failedResponse = new AppleValidation();
            failedResponse.Status = 500;
            
            return failedResponse;
        }

        if (response.Status == 21007)
        {
            Log.Warn(owner: Owner.Nathan, message: "Apple receipt validation failed. Falling back to attempt validating in sandbox...");
            _apiService
                .Request(_dynamicConfig.Require<string>("iosVerifyReceiptSandbox"))
                .SetPayload(new RumbleJson
                            {
                                { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                                { "password", sharedSecret }
                            })
                .Post(out RumbleJson sbResponse, out int sandboxCode);

            AppleValidation sandboxResponse = sbResponse.ToModel<AppleValidation>();
            
            if (!sandboxCode.Between(200, 299))
            {
                Log.Error(owner: Owner.Nathan, message: "Request to the Apple's App Store sandbox failed. App store is down.", data: $"Code: {code}.");
                AppleValidation failedResponse = new AppleValidation();
                failedResponse.Status = 500;
                
                return failedResponse;
            }

            if (sandboxResponse.Status != 0)
            {
                Log.Error(owner: Owner.Nathan, message: "Failed to validate iOS receipt in sandbox. Apple's App store may be down.", data: $"Status: {sandboxResponse.Status}");
            }

            return sandboxResponse;
        }

        if (response.Status != 0)
        {
            Log.Error(owner: Owner.Nathan, message: "Failed to validate iOS receipt.");
            throw new PlatformException("Failed to validate iOS receipt.");
        }

        return response;
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

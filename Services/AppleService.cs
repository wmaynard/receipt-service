using System;
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
    private readonly ApiService _apiService;

    public AppleService(ApiService apiService)
    {
        _apiService = apiService;
    }

    // apple specific looks at receipt
    // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
    // requires password
    // requires exclude-old-transactions if auto-renewable subscriptions
    // assuming no subscriptions for now, possible to put in later if needed
    public AppleVerificationResult VerifyApple(string receipt, string signature = null)
    {
        AppleValidation verified = VerifyAppleData(receipt);

        if (verified.Status == 0)
        {
            return new AppleVerificationResult
                   {
                       Status = "success",
                       Response = verified.Receipt,
                       TransactionId = verified.Receipt.InApp[0].TransactionId,
                       ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{verified.Receipt.InApp[0].TransactionId}",
                       ReceiptData = verified.Receipt.JSON,
                       Timestamp = Convert.ToInt64(verified.Receipt.ReceiptCreationDateMs)
                   };
        }
        return new AppleVerificationResult
               {
                   Status = "failed",
                   Response = verified?.Receipt,
                   TransactionId = verified?.Receipt.InApp[0].TransactionId,
                   ReceiptKey = null,
                   ReceiptData = verified?.Receipt.JSON,
                   Timestamp = Convert.ToInt64(verified?.Receipt.ReceiptCreationDateMs)
               };
    }

    // Sends the request to attempt to verify receipt data
    public AppleValidation VerifyAppleData(string receipt) // apple takes stringified version of receipt, includes receipt-data, password
    {
        _apiService
            .Request(PlatformEnvironment.Require(key: "iosVerifyReceiptUrl"))
            .SetPayload(new RumbleJson
            {
                { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                { "password", PlatformEnvironment.Require(key: "sharedSecret") }
            })
            .Post(out AppleValidation response, out int code);

        if (!code.Between(200, 299))
        {
            Log.Error(owner: Owner.Nathan, message: $"Request to the Apple's App Store failed with code {code}.");
            throw new PlatformException("Request to the Apple's App Store failed.");
        }

        if (response.Status == 21007)
        {
            Log.Warn(owner: Owner.Nathan, message: "Apple receipt validation failed. Falling back to attempt validating in sandbox...");
            _apiService
                .Request(PlatformEnvironment.Require<string>("iosVerifyReceiptSandbox"))
                .SetPayload(new RumbleJson
                            {
                                { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                                { "password", PlatformEnvironment.Require<string>(key: "sharedSecret") }
                            })
                .Post(out RumbleJson sbResponse, out int sandboxCode);

            AppleValidation sandboxResponse = sbResponse.ToModel<AppleValidation>();
            
            if (!sandboxCode.Between(200, 299))
            {
                Log.Error(owner: Owner.Nathan, message: $"Request to the Apple's App Store sandbox failed with code {code}.");
                throw new PlatformException("Request to the Apple's App Store sandbox failed.");
            }

            if (sandboxResponse.Status != 0)
            {
                Log.Error(owner: Owner.Nathan, message: "Failed to validate iOS receipt in sandbox.");
                throw new PlatformException("Failed to validate iOS receipt in sandbox.");
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

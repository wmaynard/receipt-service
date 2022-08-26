using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class AppleService : VerificationService
{
    private readonly ApiService _apiService;

    public AppleService(ApiService apiService) => _apiService = apiService;
    // apple specific looks at receipt
    // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
    // requires password
    // requires exclude-old-transactions if auto-renewable subscriptions
    // assuming no subscriptions for now, possible to put in later if needed
    public VerificationResult VerifyApple(Receipt receipt, string signature = null)
    {
        AppleValidation verified = VerifyAppleData(receipt);

        if (verified?.Status == 0)
            throw new ReceiptException(receipt, "Failed to validate iTunes receipt.");
        
        return new VerificationResult
        {
            Status = "success",
            Response = receipt,
            TransactionId = receipt.OrderId,
            ReceiptKey = $"{PlatformEnvironment.Deployment}_s_iosReceipt_{receipt.OrderId}",
            ReceiptData = receipt.JSON,
            Timestamp = receipt.PurchaseTime
        };
    }

    public AppleValidation VerifyAppleData(Receipt receipt) // apple takes stringified version of receipt, includes receipt-data, password
    {
        AppleValidation output = null;
        
        _apiService
            .Request(PlatformEnvironment.Require<string>("iosVerifyReceiptUrl"))
            .SetPayload(new GenericData
            {
                { "receipt-data", receipt.JSON }, // does this need Encoding.UTF8.GetBytes()?
                { "password", PlatformEnvironment.Require<string>(key: "sharedSecret") }
            })
            .Post(out GenericData response, out int code);

        if (!code.Between(200, 299))
            throw new PlatformException("Failed to verify iTunes receipt.");

        // TODO: convert response to output
        return output;
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

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Services;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Controllers;

[ApiController, Route("commerce"), RequireAuth]
public class TopController : PlatformController
{
#pragma warning disable
    private readonly Services.ReceiptService _receiptService;
    private readonly ForcedValidationService _forcedValidationService;
    private readonly DynamicConfig _dynamicConfig;
#pragma warning restore

    // Attempts to verify a provided receipt
    [HttpPost, Route("receipt")]
    public ObjectResult ValidateReceipt()
    {
        string channel = Require<string>("channel");
        
        // Unlike most endpoints in Platform, there are more variables found in the methods here.
        // It would be MUCH better practice to split up Android and iOS into separate endpoints rather than have
        // branching execution with shared keys.
        return channel switch
        {
            "aos" => ValidateGoogleReceipt(),
            "ios" => ValidateAppleReceipt(),
            _ => throw new PlatformException("Receipt called with invalid channel.  Please use 'ios' or 'aos'.")
        };
    }

    [HttpPost, Route("google")]
    public ObjectResult ValidateGoogleReceipt()
    {
        string accountId = Require<string>("account");
        string signature = Require<string>("signature");
        RumbleJson rawData = Require<RumbleJson>("receipt");
        Receipt receipt = rawData.ToModel<Receipt>();
        receipt.AccountId = accountId;
        
        SuccessStatus status = SuccessStatus.False;
        
        if (_forcedValidationService.HasBeenForced(receipt.OrderId))
            status = _receiptService.Exists(receipt.OrderId)
                ? SuccessStatus.True
                : SuccessStatus.Duplicated;
        else if (signature == null)
            throw new ReceiptException(receipt, "Failed to verify Google receipt. No signature provided.");

        if (GoogleValidator.IsValid(rawData, signature))
            status = _receiptService.GetAccountIdFor(receipt, out string existingAccountId) switch // TODO: Increase validation count here
            {
                null or "" => SuccessStatus.True,
                _ when existingAccountId == accountId => SuccessStatus.Duplicated,
                _ => SuccessStatus.AccountIdMismatch
            };
        
        ProcessByStatus(status, receipt, accountId, logData: new
        {
            AccountId = accountId,
            Receipt = receipt,
            Source = "Google"
        });
        
        receipt.AccountId ??= accountId;
        return Ok(new RumbleJson
        {
            { "success", status }, // TODO: Can we rename this key to "status"?
            { "receipt", receipt } // TODO: Is this used by the game server?
        });
    }

    [HttpPost, Route("apple")]
    public ObjectResult ValidateAppleReceipt()
    {
        string accountId = Require<string>("account");
        string receiptAsString = Require<string>("receipt");
        string transactionId = Require<string>("transactionId");
        
        if (_forcedValidationService.HasBeenForced(transactionId))
            return CreateResponse(_receiptService.Exists(transactionId)
                ? SuccessStatus.True
                : SuccessStatus.Duplicated);

        bool isValid = PingApple(receiptAsString, accountId, transactionId, out ResponseFromApple response);

        if (response == null)
        {
            Log.Error(Owner.Will, "Null response from Apple API", data: new
            {
                AccountId = accountId,
                Receipt = receiptAsString
            });
            return CreateResponse(SuccessStatus.False);
        }

        PurchaseDetails details = response.Receipt?.PurchaseDetails?.Find(appleInApp => appleInApp.TransactionId == transactionId);

        if (details == null)
            Log.Warn(Owner.Will, "A receipt is valid, but no matching transaction ID was found in the response.", data: new
            {
                Help = "This could be an indication that someone is using a hacked client or trying to double-redeem an offer.",
                AccountId = accountId,
                AppleResponse = response
            });

        Receipt receipt = new()
        {
            AccountId = accountId,
            OrderId = transactionId,
            PackageName = response.Receipt?.BundleId,
            ProductId = response.Receipt?.PurchaseDetails?.FirstOrDefault()?.ProductId,
            PurchaseTime = Convert.ToInt64(response.Receipt?.ReceiptCreationDateMs),
            PurchaseState = 0,
            Acknowledged = false,
            Quantity = int.TryParse(details?.Quantity, out int quantity)
                ? quantity
                : 0
        };
        
        SuccessStatus status = _receiptService.GetAccountIdFor(receipt, out string existingAccountId) switch // TODO: Increase validation count here
        {
            null or "" => SuccessStatus.True,
            _ when existingAccountId == accountId => SuccessStatus.Duplicated,
            _ => SuccessStatus.AccountIdMismatch
        };
        
        ProcessByStatus(status, receipt, accountId, logData: new
        {
            AccountId = accountId,
            OrderId = transactionId,
            ReceiptAsString = receiptAsString,
            Source = "Apple"
        });
        
        return CreateResponse(status, details: details);
    }

    private bool PingApple(string receiptAsString, string accountId, string transactionId, out ResponseFromApple response)
    {
        // apple specific looks at receipt
        // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
        // requires password
        // requires exclude-old-transactions if auto-renewable subscriptions
        // assuming no subscriptions for now, possible to put in later if needed
        
        // response from apple
        // string environment (Production, Sandbox)
        // boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
        // byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
        // list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
        // list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
        // json receipt (json) of receipt sent for verification
        // int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes
        _apiService
            .Request(PlatformEnvironment.IsProd || _dynamicConfig.Require<bool>("isProd")
                ? _dynamicConfig.Require<string>("iosVerifyReceiptUrl")
                : _dynamicConfig.Require<string>("iosVerifyReceiptSandbox")
            )
            .SetPayload(new RumbleJson
            {
                {"receipt-data", receiptAsString},
                {"password", PlatformEnvironment.Require<string>("appleSharedSecret")}
            })
            .OnSuccess(response =>
            {
                string bundleId = response
                    ?.Optional<RumbleJson>(ResponseFromApple.FRIENDLY_KEY_RECEIPT)
                    ?.Optional<string>(AppleReceipt.FRIENDLY_KEY_BUNDLE_ID); 
                if (string.IsNullOrWhiteSpace(bundleId))
                    Log.Critical(Owner.Will, "No bundle ID detected!  Apple receipt validation will fail!", data: new
                    {
                        ResponseAsString = response?.AsString ?? "(empty)",
                        Code = response?.StatusCode ?? 0
                    });
            })
            .OnFailure(res =>
            {
                _apiService.Alert(
                    title: "Exception when sending a request to Apple.  App Store may be down.",
                    message: "An exception was encountered when sending a request to Apple's App store.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Response", res }
                    },
                    owner: Owner.Will
                );
            })
            .Post(out response, out int _);
            // TODO: use response.Require<int>("status") instead of a custom model here, as it's the only thing used?

        if (response?.Status == 21007)
        {
            if (!PlatformEnvironment.IsProd)
                return true;
            Log.Warn(Owner.Will, message: "A testflight purchase was attempted on the production environment. This receipt validation is thus blocked.", data: new
            {
                AccountId = accountId
            });
        }
        else if (response?.Status == 0)
            return true;
        else
            _apiService.Alert(
                title: "Failed to validate iOS receipt.",
                message: "Failed to validate iOS receipt. Apple's App store may have an outage.",
                countRequired: 1,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "Account ID", accountId },
                    { "Status", response.Status }
                },
                owner: Owner.Will
            );

        return false;
    }

    private void ProcessByStatus(SuccessStatus status, Receipt receipt, string accountId, object logData)
    {
        receipt.AccountId ??= accountId;
        switch (status)
        {
            case SuccessStatus.True:
                receipt.EnsureReceiptBundleMatches();
                
                Log.Info(Owner.Will, "Successful receipt processed.", logData);
                
                _receiptService.Insert(receipt); // TODO: Set validations to 1 on insert
                break;
            case SuccessStatus.Duplicated:
                receipt.EnsureReceiptBundleMatches();
                Log.Warn(Owner.Will, "Duplicate receipt processed with the same account ID.", logData);
                break;
            case SuccessStatus.False:
                Log.Error(Owner.Will, "Failed to validate receipt.", logData);
                break;
            case SuccessStatus.AccountIdMismatch:
                _apiService.Alert(
                    title: "Duplicate receipt processed with a different account ID.",
                    message: "Duplicate receipt processed with a different account ID. Potential malicious actor.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId }
                    } 
                );
                break;
            default:
                break;
        }
    }

    private ObjectResult CreateResponse(SuccessStatus status, Receipt receipt = null, PurchaseDetails details = null) => Ok(new RumbleJson
    {
        { "success", status },
        { "receipt", receipt != null
            ? receipt
            : details
        }
    });
}
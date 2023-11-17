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
            "aos" => ValidateAndroid(),
            "ios" => ValidateApple(),
            _ => throw new PlatformException("Receipt called with invalid channel.  Please use 'ios' or 'aos'.")
        };
    }

    [HttpPost, Route("google")]
    public ObjectResult ValidateGoogleReceipt()
    {
        string accountId = Require<string>("account");
        string signature = Require<string>("signature");
        Receipt receipt = Require<Receipt>("receipt");
        RumbleJson rawData = Require<RumbleJson>("receipt"); // There are two methods of validation; unsure if we can eliminate one of these
        // Receipt temp = rawData.ToModel<Receipt>();
        
        SuccessStatus status = SuccessStatus.False;
        
        if (_forcedValidationService.HasBeenForced(receipt.OrderId))
            status = _receiptService.Exists(receipt.OrderId)
                ? SuccessStatus.True
                : SuccessStatus.Duplicated;
        else if (signature == null)
            throw new ReceiptException(receipt, "Failed to verify Google receipt. No signature provided.");

        if (GoogleValidator.IsValid(receipt, rawData, signature))
            status = _receiptService.GetAccountIdFor(receipt.OrderId, out string existingAccountId) switch // TODO: Increase validation count here
            {
                null or "" => SuccessStatus.True,
                _ when existingAccountId == accountId => SuccessStatus.Duplicated,
                _ => SuccessStatus.DuplicatedFail
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

        SuccessStatus status = PingApple(receiptAsString, accountId, transactionId, out ResponseFromApple response);

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

        if (status == SuccessStatus.False && details == null)
            _apiService.Alert(
                title: "Failure to validate Apple receipt.",
                message: "Receipt validated correctly with Apple but no matching transaction ID was found.",
                countRequired: 5,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "Account ID", accountId },
                    { "Transaction ID", transactionId },
                    { "Receipt", receiptAsString }
                } 
            );

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
        
        ProcessByStatus(status, receipt, accountId, logData: new
        {
            AccountId = accountId,
            OrderId = transactionId,
            ReceiptAsString = receiptAsString,
            Source = "Apple"
        });
        
        return CreateResponse(status, details: details);
    }

    // TODO: Delete this once we split the endpoints.  It's WET / copied into the separate GPG receipt validation endpoint.
    private ObjectResult ValidateAndroid()
    {
        string accountId = Require<string>("account");
        string signature = Require<string>("signature");
        Receipt receipt = Require<Receipt>("receipt");
        RumbleJson rawData = Require<RumbleJson>("receipt"); // There are two methods of validation; unsure if we can eliminate one of these
        // Receipt temp = rawData.ToModel<Receipt>();
        
        SuccessStatus status = SuccessStatus.False;
        
        if (_forcedValidationService.HasBeenForced(receipt.OrderId))
            status = _receiptService.Exists(receipt.OrderId)
                ? SuccessStatus.True
                : SuccessStatus.Duplicated;
        else if (signature == null)
            throw new ReceiptException(receipt, "Failed to verify Google receipt. No signature provided.");

        if (GoogleValidator.IsValid(receipt, rawData, signature))
            status = _receiptService.GetAccountIdFor(receipt.OrderId, out string existingAccountId) switch // TODO: Increase validation count here
            {
                null or "" => SuccessStatus.True,
                _ when existingAccountId == accountId => SuccessStatus.Duplicated,
                _ => SuccessStatus.DuplicatedFail
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

    private SuccessStatus PingApple(string receiptAsString, string accountId, string transactionId, out ResponseFromApple response)
    {
        SuccessStatus output = _forcedValidationService.CheckForForcedValidation(transactionId);
        
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
            .OnSuccess(res => { } )
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
                output = SuccessStatus.True;
            else
                Log.Warn(Owner.Will, message: "A testflight purchase was attempted on the production environment. This receipt validation is thus blocked.", data: new
                {
                    AccountId = accountId
                });
        }
        else if (response?.Status == 0)
            output = _receiptService.GetAccountIdFor(transactionId, out string existingAccountId) switch
            {
                null or "" => SuccessStatus.True,
                _ when existingAccountId == accountId => SuccessStatus.Duplicated,
                _ => SuccessStatus.DuplicatedFail
            };
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

        return output;
    }

    // TODO: Delete this once we split the endpoints.  It's WET / copied into the separate Apple receipt validation endpoint.
    private ObjectResult ValidateApple()
    {
        string accountId = Require<string>("account");
        string receiptAsString = Require<string>("receipt");
        string transactionId = Require<string>("transactionId");

        SuccessStatus status = PingApple(receiptAsString, accountId, transactionId, out ResponseFromApple response);

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

        if (status == SuccessStatus.False && details == null)
            _apiService.Alert(
                title: "Failure to validate Apple receipt.",
                message: "Receipt validated correctly with Apple but no matching transaction ID was found.",
                countRequired: 5,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "Account ID", accountId },
                    { "Transaction ID", transactionId },
                    { "Receipt", receiptAsString }
                } 
            );

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
        
        ProcessByStatus(status, receipt, accountId, logData: new
        {
            AccountId = accountId,
            OrderId = transactionId,
            ReceiptAsString = receiptAsString,
            Source = "Apple"
        });
        
        return CreateResponse(status, details: details);
    }

    private void ProcessByStatus(SuccessStatus status, Receipt receipt, string accountId, object logData)
    {
        switch (status)
        {
            case SuccessStatus.True:
                Log.Info(Owner.Will, "Successful receipt processed.", logData);

                _receiptService.Create(receipt); // TODO: Set validations to 1 on insert
                break;
            case SuccessStatus.Duplicated:
                Log.Warn(Owner.Will, "Duplicate receipt processed with the same account ID.", logData);
                break;
            case SuccessStatus.False:
                Log.Error(Owner.Will, "Failed to validate receipt. Order does not exist.", logData);
                break;
            case SuccessStatus.DuplicatedFail:
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
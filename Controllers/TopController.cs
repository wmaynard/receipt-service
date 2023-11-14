using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Services;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Controllers;

[ApiController, Route(template: "commerce"), UseMongoTransaction]
public class TopController : PlatformController
{
#pragma warning disable
    private readonly Services.ReceiptService _receiptService;
    private readonly ForcedValidationService _forcedValidationService;
    private readonly VerificationService _verificationService;
#pragma warning restore

    // Attempts to verify a provided receipt
    [HttpPost, Route("receipt")]
    public ObjectResult ValidateReceipt()
    {
        string accountId = Require<string>("account");
        string channel = Require<string>("channel");
        string game = Require<string>("game");
        
        if (game != PlatformEnvironment.GameSecret)
            throw new PlatformException("Incorrect game key.", ErrorCode.Unauthorized);
        
        return channel switch
        {
            "aos" => ValidateAndroid(),
            "ios" => ValidateApple(),
            _ => throw new PlatformException("Receipt called with invalid channel.  Please use 'ios' or 'aos'.")
        };
    }

    private ObjectResult ValidateAndroid()
    {
        string accountId = Require<string>("account");
        string signature = Require<string>("signature");
        Receipt receipt = Require<Receipt>("receipt");
        RumbleJson rawData = Require<RumbleJson>("receipt"); // There are two methods of validation; unsure if we can eliminate one of these
        object logData = new
        {
            AccountId = accountId,
            Receipt = receipt
        };
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
        
        switch (status)
        {
            case SuccessStatus.True:
                Log.Info(Owner.Will, "Successful Google receipt processed.", logData);

                _receiptService.Create(receipt); // TODO: Set validations to 1 on insert
                break;
            case SuccessStatus.Duplicated:
                Log.Warn(Owner.Will, "Duplicate Google receipt processed with the same account ID.", logData);
                break;
            case SuccessStatus.False:
                Log.Warn(Owner.Will, "Failed to validate Google receipt. Order does not exist.", logData);
                break;
            case SuccessStatus.DuplicatedFail:
                _apiService.Alert(
                    title: "Duplicate Apple receipt processed with a different account ID.",
                    message: "Duplicate Apple receipt processed with a different account ID. Potential malicious actor.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId }
                    } 
                );
                break;
            case SuccessStatus.StoreOutage:
            default:
                throw new PlatformException("Unknown Android validation status");
        }
        
        receipt.AccountId ??= accountId;
        return Ok(new RumbleJson
        {
            { "success", status }, // TODO: Can we rename this key to "status"?
            { "receipt", receipt } // TODO: Is this used by the game server?
        });
    }

    private ObjectResult ValidateApple()
    {
        // TODO: Refactor ValidateApple's overload
        string accountId = Require<string>("account");
        string appleReceipt = Require<string>("receipt");
        string transactionId = Require<string>("transactionId");
                
        AppleVerificationResult appleValidated = ValidateApple(appleReceipt, accountId, transactionId, false);
        return Ok(new RumbleJson
        {
            { "success", appleValidated.Status },
            { "receipt", appleValidated.Response?.InApp.Find(inApp => inApp.TransactionId == transactionId) }
        });
    }

    // Validation process for an ios receipt
    private AppleVerificationResult ValidateApple(string receipt, string accountId, string transactionId, bool loadTest = false)
    {
        AppleVerificationResult output = _verificationService.VerifyApple(receipt: receipt, transactionId: transactionId, accountId: accountId);
            
        // response from apple
        // string environment (Production, Sandbox)
        // boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
        // byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
        // list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
        // list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
        // json receipt (json) of receipt sent for verification
        // int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes
        
        if (output?.Status == null)
            throw new AppleReceiptException(receipt, "Error occurred while trying to validate Apple receipt.");
        
        switch (output.Status)
        {
            case SuccessStatus.False:
                Log.Error(owner: Owner.Will, "Failed to validate Apple receipt. Order does not exist.", data: new
                {
                    AccountId = accountId
                });
                break;
            case SuccessStatus.DuplicatedFail:
                _apiService.Alert(
                    title: "Duplicate Apple receipt processed with a different account ID.",
                    message: "Duplicate Apple receipt processed with a different account ID. Potential malicious actor.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId }
                    } 
                );
                break;
            case SuccessStatus.Duplicated when loadTest:
            case SuccessStatus.True:
                Log.Info(Owner.Will, "Successful Apple receipt processed.", data: new
                {
                    AccountId = accountId,
                    Receipt = receipt
                });
                
                Receipt newReceipt = new()
                {
                    AccountId = accountId,
                    OrderId = output.TransactionId,
                    PackageName = output.Response.BundleId,
                    ProductId = output.Response.InApp[0].ProductId,
                    PurchaseTime = output.Timestamp,
                    PurchaseState = 0,
                    Acknowledged = false
                };
                string quantity = output.Response.InApp.Find(inApp => inApp.TransactionId == transactionId)?.Quantity;
                if (!string.IsNullOrWhiteSpace(quantity))
                    newReceipt.Quantity = int.Parse(quantity);

                _receiptService.Create(newReceipt);
                break;
            case SuccessStatus.Duplicated:
                Log.Warn(Owner.Will, "Duplicate Apple receipt processed with the same account ID.", data: new
                {
                    AccountId = accountId,
                    Receipt = receipt
                });
                break;
        }

        return output;
    }
}
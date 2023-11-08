using System;
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
    [HttpPost, Route(template: "receipt")]
    public ObjectResult ReceiptVerify()
    {
        string accountId = Require<string>(key: "account");
        string channel = Require<string>(key: "channel");
        string game = Require<string>(key: "game");
        
        if (game != PlatformEnvironment.GameSecret)
            throw new PlatformException("Incorrect game key.", code: ErrorCode.Unauthorized);

        bool loadTest = !PlatformEnvironment.IsProd && Optional<bool>("loadTest");

        switch (channel)
        {
            case "aos":
                string signature = Optional<string>(key: "signature"); // for android
                Receipt receipt = Require<Receipt>(key: "receipt");
                RumbleJson receiptData = Require<RumbleJson>(key: "receipt"); // for android fallback to verify raw data
                
                if (string.IsNullOrWhiteSpace(signature))
                    throw new ReceiptException(receipt, "Receipt called with 'aos' as the channel without a signature. 'aos' receipts require a signature");
                VerificationResult validated = ValidateAndroid(receipt, accountId, signature, receiptData, loadTest);
                return Ok(new RumbleJson
                {
                    { "success", validated.Status },
                    { "receipt", validated.Response }
                });
            case "ios":
                string appleReceipt = Require<string>(key: "receipt");
                string transactionId = Require<string>(key: "transactionId");
                AppleVerificationResult appleValidated = ValidateApple(appleReceipt, accountId, transactionId, loadTest);
                return Ok(new RumbleJson
                {
                    { "success", appleValidated.Status },
                    { "receipt", appleValidated.Response?.InApp.Find(inApp => inApp.TransactionId == transactionId) }
                });
            default:
                throw new PlatformException("Receipt called with invalid channel.  Please use 'ios' or 'aos'.");
        }
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
            case AppleVerificationResult.SuccessStatus.False:
                Log.Error(owner: Owner.Will, "Failed to validate Apple receipt. Order does not exist.", data: new
                {
                    AccountId = accountId
                });
                break;
            case AppleVerificationResult.SuccessStatus.DuplicatedFail:
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
            case AppleVerificationResult.SuccessStatus.Duplicated when loadTest:
            case AppleVerificationResult.SuccessStatus.True:
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
            case AppleVerificationResult.SuccessStatus.Duplicated:
                Log.Warn(Owner.Will, "Duplicate Apple receipt processed with the same account ID.", data: new
                {
                    AccountId = accountId,
                    Receipt = receipt
                });
                break;
        }

        return output;
    }

    // Validation process for an aos receipt
    private VerificationResult ValidateAndroid(Receipt receipt, string accountId, string signature, RumbleJson receiptData, bool loadTest = false)
    {
        receipt.Validate();
        
        // Per https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products/get:
        // This API call should be able to check the purchase and consumption status of an inapp item.
        // This should be the modern way of validating receipts.  Consequently it should be left in as a
        // comment in case Google ever drops support for our current receipt validation.  However, it should
        // be noted that this call is missing its auth header; Sean may be needed to get the correct auth token.
        // _apiService
        //     .Request($"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{receipt.PackageName}/purchases/products/{receipt.ProductId}/tokens/{receipt.PurchaseToken}")
        //     .OnFailure(response => Log.Local(Owner.Will, response.AsGenericData.JSON, emphasis: Log.LogType.ERROR))
        //     .Get(out GenericData json, out int code);
        VerificationResult output = _verificationService.VerifyGoogle(receipt, signature, receiptData, accountId);
        receipt.AccountId = accountId;
        
        if (output?.Status == null)
            throw new ReceiptException(receipt, message: "Error occurred while trying to validate Google receipt.");

        switch (output.Status)
        {
            case VerificationResult.SuccessStatus.False:
                Log.Error(Owner.Will, "Failed to validate Google receipt. Order does not exist.", data: new
                {
                    AccountId = accountId
                });
                break;
            case VerificationResult.SuccessStatus.DuplicatedFail:
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
            case VerificationResult.SuccessStatus.Duplicated when loadTest:
            case VerificationResult.SuccessStatus.True:
                Log.Info(Owner.Will, "Successful Google receipt processed.", data: new
                {
                    AccountId = accountId,
                    Receipt = receipt
                });

                _receiptService.Create(receipt);
                break;
            case VerificationResult.SuccessStatus.Duplicated:
                Log.Warn(Owner.Will, "Duplicate Google receipt processed with the same account ID.", data: new
                {
                    AccountId = accountId,
                    Receipt = receipt
                });
                break;
        }

        return output;
    }
}
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

namespace Rumble.Platform.ReceiptService.Controllers;

[ApiController, Route(template: "commerce"), UseMongoTransaction]
public class TopController : PlatformController
{
#pragma warning disable
    private readonly AppleService            _appleService;
    private readonly Services.ReceiptService _receiptService;
    private readonly GoogleService           _googleService;
#pragma warning restore

    // Attempts to verify a provided receipt
    [HttpPost, Route(template: "receipt")]
    public ObjectResult ReceiptVerify()
    {
        string accountId = Require<string>(key: "account");
        string channel = Require<string>(key: "channel");
        string game = Require<string>(key: "game");
        
        if (game != PlatformEnvironment.GameSecret)
        {
            throw new PlatformException("Incorrect game key.", code: ErrorCode.Unauthorized);
        }

        switch (channel)
        {
            case "aos":
                string signature = Optional<string>(key: "signature"); // for android
                Receipt receipt = Require<Receipt>(key: "receipt");
                RumbleJson receiptData = Require<RumbleJson>(key: "receipt"); // for android fallback to verify raw data
                Log.Info(owner: Owner.Nathan, message: $"Receipt parsed from receipt data", data: $"Receipt: {receipt.JSON}");
                
                if (signature == null)
                {
                    throw new ReceiptException(receipt,
                                               "Receipt called with 'aos' as the channel without a signature. 'aos' receipts require a signature");
                }
                VerificationResult validated = ValidateAndroid(receipt, accountId, signature, receiptData);
                return Ok(new RumbleJson
                          {
                              {"success", validated.Status},
                              {"receipt", validated.Response}
                          });
            case "ios":
                string appleReceipt = Require<string>(key: "receipt");
                string transactionId = Require<string>(key: "transactionId");
                AppleVerificationResult appleValidated = ValidateApple(appleReceipt, accountId, transactionId);
                return Ok(new RumbleJson
                          {
                              {"success", appleValidated.Status},
                              {"receipt", appleValidated.Response.InApp.Find(inApp => inApp.TransactionId == transactionId)}
                          });
            default:
                throw
                    new PlatformException(message: "Receipt called with invalid channel.  Please use 'ios' or 'aos'.");
        }
    }

    // Validation process for an ios receipt
    private AppleVerificationResult ValidateApple(string receipt, string accountId, string transactionId)
    {
        AppleVerificationResult output = _appleService.VerifyApple(receipt: receipt, transactionId: transactionId, accountId: accountId);
            
        // response from apple
        // string environment (Production, Sandbox)
        // boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
        // byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
        // list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
        // list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
        // json receipt (json) of receipt sent for verification
        // int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes
        
        switch (output?.Status)
        {
            case null:
                throw new AppleReceiptException(receipt, message: "Error occurred while trying to validate Apple receipt.");
            case AppleVerificationResult.SuccessStatus.False:
                Log.Error(owner: Owner.Nathan, message: "Failed to validate Apple receipt. Order does not exist.");
                break;
            case AppleVerificationResult.SuccessStatus.DuplicatedFail:
                Log.Error(owner: Owner.Nathan, message: "Duplicate Apple receipt processed with a different account ID.");
                break;
            case AppleVerificationResult.SuccessStatus.Duplicated:
                Log.Warn(owner: Owner.Nathan, message: "Duplicate Apple receipt processed with the same account ID.", data: receipt);
                break;
            case AppleVerificationResult.SuccessStatus.True:
                Log.Info(owner: Owner.Nathan, message: "Successful Apple receipt processed.", data: output.Response);

                Receipt newReceipt = new Receipt();

                newReceipt.AccountId = accountId;
                newReceipt.OrderId = output.TransactionId;
                newReceipt.PackageName = output.Response.BundleId;
                newReceipt.ProductId = output.Response.InApp[0].ProductId;
                newReceipt.PurchaseTime = output.Timestamp;
                newReceipt.PurchaseState = 0;
                string quantity = output.Response.InApp.Find(inApp => inApp.TransactionId == transactionId)
                                        ?.Quantity;
                if (quantity != null)
                {
                    newReceipt.Quantity =
                        Int32.Parse(quantity);
                }

                newReceipt.Acknowledged = false;

                _receiptService.Create(newReceipt);
                break;
        }

        return output;
    }

    // Validation process for an aos receipt
    private VerificationResult ValidateAndroid(Receipt receipt, string accountId, string signature, RumbleJson receiptData)
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
        VerificationResult output = _googleService.VerifyGoogle(receipt: receipt, signature: signature, receiptData: receiptData, accountId: accountId);

        switch (output?.Status)
        {
            case null:
                throw new ReceiptException(receipt: receipt, message: "Error occurred while trying to validate Google receipt.");
            case VerificationResult.SuccessStatus.False:
                Log.Error(owner: Owner.Nathan, message: "Failed to validate Google receipt. Order does not exist.");
                break;
            case VerificationResult.SuccessStatus.DuplicatedFail:
                Log.Error(owner: Owner.Nathan, message: "Duplicate Google receipt processed with a different account ID.");
                break;
            case VerificationResult.SuccessStatus.Duplicated:
                Log.Warn(owner: Owner.Nathan, message: "Duplicate Google receipt processed with the same account ID.", data: receipt);
                break;
            case VerificationResult.SuccessStatus.True:
                Log.Info(owner: Owner.Nathan, message: "Successful Google receipt processed.", data: receipt);

                receipt.AccountId = accountId;

                _googleService.Create(receipt);
                break;
        }

        return output;
    }
}
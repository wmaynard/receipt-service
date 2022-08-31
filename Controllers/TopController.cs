using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Exceptions;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Services;

namespace Rumble.Platform.ReceiptService.Controllers;

[ApiController, Route(template: "commerce"), UseMongoTransaction]
public class TopController : PlatformController
{
#pragma warning disable
    private readonly AppleService _appleService;
    private readonly GoogleService _googleService;
#pragma warning restore

    [HttpPost, Route(template: "receipt")]
    public ObjectResult ReceiptVerify()
    {
        string accountId = Require<string>(key: "account");
        string channel = Require<string>(key: "channel");
        string game = Require<string>(key: "game");
        string signature = Optional<string>(key: "signature");
        Receipt receipt = Require<Receipt>(key: "receipt");

        if (channel == "aos" && signature == null)
        {
            throw new ReceiptException(receipt,
                                       "Receipt called with 'aos' as the channel without a signature. 'aos' receipts require a signature");
        }

        // remove when ios iaps are implemented on client/server
        if (channel == "ios")
        {
            throw new PlatformException(message: "IOS IAPs shouldn't exist yet...?");
        }

        // the following are the current payload keys
        // if we need receipt to contain everything, perhaps key:receipt is not a receipt yet, but create receipt using these
        // also optional string signature for android

        // string receiptData = Require<string>(key: "receipt"); // is stringified in the request
        // Receipt receipt = JsonConvert.DeserializeObject<Receipt>(receiptData);
        
        // Log.Info(owner: Owner.Nathan, message: $"Receipt validation request", data: $"game: {game}, accountId: {accountId}, channel: {channel}, receiptData: {receiptData}");
        Log.Info(owner: Owner.Nathan, message: $"Receipt parsed from receipt data", data: $"Receipt: {receipt.JSON}");

        if (game != PlatformEnvironment.GameSecret)
            throw new PlatformException("Incorrect game key.", code: ErrorCode.Unauthorized);

        VerificationResult validated = channel switch
        {
            "aos" => ValidateAndroid(receipt, accountId, signature),
            "ios" => ValidateApple(receipt, accountId),
            _ => throw new ReceiptException(receipt, "Receipt called with invalid channel.  Please use 'ios' or 'aos'.")
        };

        return Ok(receipt.ResponseObject);
    }

    private VerificationResult ValidateApple(Receipt receipt, string accountId)
    {
        receipt.Validate();
        
        VerificationResult output = _appleService.VerifyApple(receipt: receipt);
            
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
                throw new ReceiptException(receipt, message: "Error validating Apple receipt.");
            case "failed":
                throw new ReceiptException(receipt, message: "Failed to validate Apple receipt. Order does not exist.");
            case "success":
                Log.Info(owner: Owner.Nathan, message: "Successful Apple receipt processed.");

                if (_appleService.Exists(receipt?.OrderId))
                {
                    throw new ReceiptException(receipt, "Apple receipt has already been redeemed.");
                }

                receipt.AccountId = accountId;

                _appleService.Create(receipt);
                break;
        }

        return output;
    }

    private VerificationResult ValidateAndroid(Receipt receipt, string accountId, string signature)
    {
        receipt.Validate();
        VerificationResult output = _googleService.VerifyGoogle(receipt: receipt, signature: signature);

        switch (output?.Status)
        {
            case null:
                throw new ReceiptException(receipt, message: "Error validating Google receipt.");
            case "failed":
                throw new ReceiptException(receipt, message: "Failed to validate Google receipt. Order does not exist.");
            case "success":
                Log.Info(owner: Owner.Nathan, message: "Successful Google receipt processed.");

                if (_googleService.Find(filter: receipt => receipt.OrderId == output.TransactionId).FirstOrDefault() != null)
                {
                    throw new ReceiptException(receipt, "Google receipt has already been redeemed.");
                }

                receipt.AccountId = accountId;

                _googleService.Create(receipt);
                break;
        }

        return output;
    }
}
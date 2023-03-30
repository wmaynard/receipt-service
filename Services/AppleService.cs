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
    private readonly ApiService              _apiService;
    private readonly ReceiptService          _receiptService;
    private readonly DynamicConfig           _dynamicConfig;
    private readonly ForcedValidationService _forcedValidationService;
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
        AppleValidation verified = VerifyAppleData(receipt, accountId);

        bool forceValidation = _forcedValidationService.CheckTransactionId(transactionId);
        
        if (forceValidation)
        {
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

        if (verified.Status == 0)
        {
            AppleInApp inApp = verified.Receipt.InApp.Find(appleInApp => appleInApp.TransactionId == transactionId);
            if (inApp == null)
            {
                Log.Error(owner: Owner.Nathan, message: "Receipt validated correctly with Apple but no matching transaction ID was found.", data: $"Account ID: {accountId}. Request transaction ID: {transactionId}. Receipt: {receipt}.");
                
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
                           Response = verified?.Receipt,
                           TransactionId = transactionId,
                           ReceiptKey = null,
                           ReceiptData = verified?.Receipt.JSON,
                           Timestamp = Convert.ToInt64(verified?.Receipt.ReceiptCreationDateMs)
                       };
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
                Log.Warn(owner: Owner.Nathan, message: "Duplicated receipt processed but account IDs did not match.", data: $"Account ID: {accountId}. Receipt: {receipt}");
                
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

        if (verified.Status == 21003 || verified.Status == 21007) // failed to authenticate or testflight on prod. Apple returns nothing but the status
        {
            return new AppleVerificationResult
               {
                   Status = AppleVerificationResult.SuccessStatus.False,
                   Response = null,
                   TransactionId = transactionId,
                   ReceiptKey = null,
                   ReceiptData = null,
                   Timestamp = 0
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
               Response = null,
               TransactionId = transactionId,
               ReceiptKey = null,
               ReceiptData = null,
               Timestamp = 0
           };
    }

    // Sends the request to attempt to verify receipt data
    public AppleValidation VerifyAppleData(string receipt, string accountId) // apple takes stringified version of receipt, includes receipt-data, password
    {
        string sharedSecret = PlatformEnvironment.Require(key: "appleSharedSecret"); // for some reason this is trying to get from request payload

        AppleValidation response;
        int code;
        try
        {
            _apiService
                .Request(_dynamicConfig.Require<string>(key: "iosVerifyReceiptUrl"))
                .SetPayload(new RumbleJson
                            {
                                { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                                { "password", sharedSecret }
                            })
                .Post(out response, out code);
        }
        catch (Exception e)
        {
            Log.Error(owner: Owner.Nathan, message: "An exception was encountered when sending a request to Apple's App store.", data: $"Account ID: {accountId}. Exception: {e}");
            
            _apiService.Alert(
                title: "Exception when sending a request to Apple.",
                message: "An exception was encountered when sending a request to Apple's App store.",
                countRequired: 1,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "Account ID", accountId },
                    { "Exception", e}
                } 
            );

            
            AppleValidation failedResponse = new AppleValidation();
            failedResponse.Status = 500;
            
            return failedResponse;
        }

        if (!code.Between(200, 299))
        {
            Log.Error(owner: Owner.Nathan, message: "Request to Apple's App store failed. Apple's App store is down.", data:$"Account ID: {accountId}. Code: {code}");

            _apiService.Alert(
                title: "Request to Apple's App store failed.",
                message: "Request to Apple's App store failed. Apple's App store is down.",
                countRequired: 1,
                timeframe: 300,
                data: new RumbleJson
                {
                    { "Account ID", accountId },
                    { "Code", code}
                } 
            );
            
            AppleValidation failedResponse = new AppleValidation();
            failedResponse.Status = 500;
            
            return failedResponse;
        }

        string isProd = _dynamicConfig.Require<string>(key: "isProd"); // doesn't seem like dc has booleans

        if (response.Status == 21007 && !PlatformEnvironment.IsProd && isProd != "true")
        {
            Log.Warn(owner: Owner.Nathan, message: "Apple receipt validation failed. Falling back to attempt validating in sandbox...", data: $"Account ID: {accountId}.");
            
            RumbleJson sbResponse;
            int sandboxCode;
            
            try
            {
                _apiService
                    .Request(_dynamicConfig.Require<string>("iosVerifyReceiptSandbox"))
                    .SetPayload(new RumbleJson
                                {
                                    { "receipt-data", receipt }, // does this need Encoding.UTF8.GetBytes()?
                                    { "password", sharedSecret }
                                })
                    .Post(out sbResponse, out sandboxCode);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: "An exception was encountered when sending a request to Apple's App store sandbox.", data: $"Account ID: {accountId}. Exception: {e}");
            
                _apiService.Alert(
                    title: "Exception when sending a request to Apple.",
                    message: "An exception was encountered when sending a request to Apple's App store sandbox.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Exception", e}
                    } 
                );
                
                AppleValidation failedResponse = new AppleValidation();
                failedResponse.Status = 500;
            
                return failedResponse;
            }

            AppleValidation sandboxResponse = sbResponse.ToModel<AppleValidation>();
            
            if (!sandboxCode.Between(200, 299))
            {
                Log.Error(owner: Owner.Nathan, message: "Request to the Apple's App Store sandbox failed. Apple's App store is down.", data: $"Account ID: {accountId}. Code: {sandboxCode}.");
                
                _apiService.Alert(
                    title: "Request to Apple's App store sandbox failed.",
                    message: "Request to Apple's App store sandbox failed. Apple's App store sandbox is down.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Code", code}
                    } 
                );
                
                AppleValidation failedResponse = new AppleValidation();
                failedResponse.Status = 500;
                
                return failedResponse;
            }

            if (sandboxResponse.Status != 0)
            {
                Log.Error(owner: Owner.Nathan, message: "Failed to validate iOS receipt in sandbox. Apple's App store may have an outage.", data: $"Account ID: {accountId}. Status: {sandboxResponse.Status}");
                
                _apiService.Alert(
                    title: "Failed to validate iOS receipt in sandbox.",
                    message: "Failed to validate iOS receipt in sandbox. Apple's App store may have an outage.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        { "Account ID", accountId },
                        { "Status", sandboxResponse.Status}
                    } 
                );
            }

            return sandboxResponse;
        }

        if (response.Status == 21007 && (PlatformEnvironment.IsProd || isProd == "true"))
        {
            Log.Warn(owner: Owner.Nathan, message: "A testflight purchase was attempted on the production environment. This receipt validation is thus blocked.", data: $"Account ID: {accountId}.");
        }

        if (response.Status != 0)
        {
            Log.Error(owner: Owner.Nathan, message: "Failed to validate iOS receipt. Apple's App store may have an outage.", data: $"Account ID: {accountId}. Status: {response.Status}");

            if (response.Status != 21007)
            {
                _apiService.Alert(
                    title: "Failed to validate iOS receipt.",
                    message: "Failed to validate iOS receipt. Apple's App store may have an outage.",
                    countRequired: 1,
                    timeframe: 300,
                    data: new RumbleJson
                    {
                        {"Account ID", accountId},
                        {"Status", response.Status}
                    }
                );
            }
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

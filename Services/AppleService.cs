using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services
{
    public class AppleService : VerificationService
    {
        // apple specific looks at receipt and game
        // receipt is base64 encoded, supposedly fetched from app on device with NSBundle.appStoreReceiptURL
        // requires password
        // requires exclude-old-transactions if auto-renewable subscriptions
        public async Task<VerificationResult> VerifyApple(Receipt receipt, string accountId = null, string signature = null)
        {
            VerificationResult verification = null;
            AppleValidation verified = null;
            try
            {
                verified = await VerifyAppleData(receipt: receipt, env: "prod");
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: e.Message);
                try
                {
                    verified = await VerifyAppleData(receipt: receipt, env: "sandbox");
                }
                catch (Exception exception)
                {
                    Log.Error(owner: Owner.Nathan, message: $"Failed to validate iTunes receipt against env sandbox. {e.Message}. Receipt {receipt.JSON}");
                }
            }

            if (verified?.Status == 0)
            {
                string receiptKey = $"{PlatformEnvironment.Variable(name: "RUMBLE_DEPLOYMENT")}_s_iosReceipt_{receipt.OrderId}";
                
                verification = new VerificationResult(
                    status: "success",
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: receiptKey,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
            }
            else
            {
                verification = new VerificationResult(
                    status: "failed",
                    response: receipt,
                    transactionId: receipt.OrderId,
                    offerId: receipt.ProductId,
                    receiptKey: null,
                    receiptData: receipt.JSON,
                    timestamp: receipt.PurchaseTime
                );
                Log.Error(owner: Owner.Nathan, message: $"Failure to validate iTunes receipt. Receipt: {receipt.JSON}");
            }
            return verification;
        }

        public async Task<AppleValidation> VerifyAppleData(Receipt receipt, string env) // apple takes stringified version of receipt, includes receipt-data, password
        {
            AppleValidation response = null;
            string reqUri = env == "prod"
                ? PlatformEnvironment.Variable(name: "iosVerifyReceiptUrl")
                : PlatformEnvironment.Variable(name: "iosVerifyReceiptSandbox");

            byte[] receiptData = Encoding.UTF8.GetBytes(receipt.JSON);
            string password = PlatformEnvironment.Variable(name: "sharedSecret");

            Dictionary<string, object> reqObj = new Dictionary<string, object>
            {
                {"receipt-data", receiptData},
                {"password", password}
            };

            string reqJson = JsonConvert.SerializeObject(reqObj);

            JsonContent reqData = JsonContent.Create(reqJson);
            
            HttpResponseMessage httpResponse = await client.PostAsync(requestUri: reqUri, content: reqData);
            // TODO
            // will require a valid apple receipt to test
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<AppleValidation>(responseBody);
            }
            else
            {
                throw new Exception(message: $"Failed to verify iTunes receipt. HTTP {httpResponse.StatusCode} with reason {httpResponse.ReasonPhrase}.");
            }
            if (response?.Status == 21007) // fallback to sandbox
            {
                throw new Exception(message: $"Failed to verify iTunes receipt with environment {env}. Status code 21007 (sandbox).");
            }

            return response;
        }
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

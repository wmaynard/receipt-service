using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
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
            string receiptData = receipt.ToJson();
            try
            {
                verification = await VerifyAppleData(receiptData: receiptData, env: "prod");
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: e.Message);
                try
                {
                    verification = await VerifyAppleData(receiptData: receiptData, env: "sandbox");
                }
                catch (Exception exception)
                {
                    Log.Error(owner: Owner.Nathan, message: $"Failed to validate iTunes receipt against env sandbox. Receipt {receiptData}");
                }
            }

            return verification;
        }

        public async Task<VerificationResult> VerifyAppleData(string receiptData, string env) // apple takes stringified version of receipt, includes receipt-data, password
        {
            VerificationResult response = null;
            string reqUri = env == "prod"
                ? PlatformEnvironment.Variable(name: "iosVerifyReceiptUrl")
                : PlatformEnvironment.Variable(name: "iosVerifyReceiptSandbox");
            HttpContent receipt = new StringContent(JsonConvert.SerializeObject(receiptData));
            HttpResponseMessage httpResponse = await client.PostAsync(requestUri: reqUri, content: receipt);
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                response = JsonConvert.DeserializeObject<VerificationResult>(httpResponse.ToJson());
            }
            else
            {
                throw new Exception(message: $"Failed to verify iTunes receipt. HTTP {httpResponse.StatusCode} with reason {httpResponse.ReasonPhrase}.");
            }
            if (response.Status == "21007") // fallback to sandbox
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services
{
    public class SamsungService : VerificationService
    {
        // samsung specific looks at receipt, game, playergukey(accountid)? not actually used for now
        public async Task<VerificationResult> VerifySamsung(Receipt receipt, string accountId = null, string signature = null)
        {
            VerificationResult verification = null;
            SamsungValidation verified = null;
            
            try
            {
                verified = await VerifySamsungData(receipt: receipt, accountId: accountId);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to validate Samsung receipt. Receipt {receipt.JSON}");
            }

            if (verified?.Status == "true")
            {
                string receiptKey = $"{PlatformEnvironment.Variable(name: "RUMBLE_DEPLOYMENT")}_s_samsungReceipt_{receipt.OrderId}";

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
                Log.Error(owner: Owner.Nathan, message: $"Failure to validate Samsung receipt. Receipt: {receipt.JSON}");
            }
            return verification;
        }
        
        public async Task<SamsungValidation> VerifySamsungData(Receipt receipt, string accountId)
        {
            SamsungValidation response = null;
            // POST request to the previously used url for kingsroad gives a 405 method not allowed
            string reqUri = PlatformEnvironment.Variable(name: "samsungVerifyReceiptUrl");

            string receiptString = receipt.JSON;
            string protocolVersion = "2.0";

            Dictionary<string, string> reqObj = new Dictionary<string, string>
            {
                {"purchaseID", receiptString},
                {"protocolVersion", protocolVersion}
            };

            string reqJson = JsonConvert.SerializeObject(reqObj);

            JsonContent reqData = JsonContent.Create(reqJson);
            
            HttpResponseMessage httpResponse = await client.PostAsync(requestUri: reqUri, content: reqData);
            // TODO
            // will require a valid samsung receipt to test
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<SamsungValidation>(responseBody);
            }
            else
            {
                throw new Exception(message: $"Failed to verify Samsung receipt. Http {httpResponse.StatusCode} with reason {httpResponse.ReasonPhrase}.");
            }
            
            return response;
        }
    }
}
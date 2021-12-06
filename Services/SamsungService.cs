using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services
{
    public class SamsungService : VerificationService
    {
        // samsung specific looks at receipt, playergukey(accountid)? used in old ver for testing
        public async Task<VerificationResult> VerifySamsung(Receipt receipt, string signature = null)
        {
            VerificationResult verification = null;
            SamsungValidation verified = null;
            
            try
            {
                verified = await VerifySamsungData(receipt: receipt);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to validate Samsung receipt. {e.Message}. Receipt {receipt.JSON}");
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
                Log.Error(owner: Owner.Nathan, message: $"Failed to validate Samsung receipt. Receipt: {receipt.JSON}");
            }
            return verification;
        }
        
        public async Task<SamsungValidation> VerifySamsungData(Receipt receipt)
        {
            SamsungValidation response = null;
            // POST request to the previously used url for kingsroad gives a 405 method not allowed
            // new one appears to be https://iap.samsungapps.com/iap/v6/receipt?purchaseID={PurchaseId}, but not specified if get/post, assume get cause of id in link
            // purchase id might be orderid?
            string reqUriRoot = PlatformEnvironment.Variable(name: "samsungVerifyReceiptUrl");
            string reqUri = reqUriRoot + receipt.OrderId;

            // the following is old version
            /*
            string receiptString = receipt.JSON;
            string protocolVersion = "2.0";

            Dictionary<string, string> reqObj = new Dictionary<string, string>
            {
                // yes purchase id takes receipt in string form in the old version (why?)
                {"purchaseID", receiptString},
                {"protocolVersion", protocolVersion}
            };

            string reqJson = JsonConvert.SerializeObject(reqObj);
            JsonContent reqData = JsonContent.Create(reqJson);
            HttpResponseMessage httpResponse = await client.PostAsync(requestUri: reqUri, content: reqData);
            */
            
            HttpResponseMessage httpResponse = await client.GetAsync(requestUri: reqUri); // get
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
using System;
using System.Net;
using System.Net.Http;
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
        // samsung specific looks at receipt, game, playergukey(accountid)?
        public async Task<VerificationResult> VerifySamsung(Receipt receipt, string accountId = null, string signature = null)
        {
            VerificationResult verification = null;
            string receiptData = receipt.ToJson();
            try
            {
                verification = await VerifySamsungData(receiptData: receiptData, accountId: accountId);
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: $"Failed to validate Samsung receipt. Receipt {receiptData}");
            }

            return verification;
        }
        
        public async Task<VerificationResult> VerifySamsungData(string receiptData, string accountId)
        {
            VerificationResult response = null;
            string reqUri = PlatformEnvironment.Variable(name: "samsungVerifyReceiptUrl");
            HttpContent receipt = new StringContent(JsonConvert.SerializeObject(receiptData));
            HttpResponseMessage httpResponse = await client.PostAsync(requestUri: reqUri, content: receipt);
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                response = JsonConvert.DeserializeObject<VerificationResult>(httpResponse.ToJson());
            }
            else
            {
                throw new Exception(message: $"Failed to verify Samsung receipt. Http {httpResponse.StatusCode} with reason {httpResponse.ReasonPhrase}.");
            }
            
            return response;
        }
    }
}
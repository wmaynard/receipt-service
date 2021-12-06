using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.ReceiptService.Models
{
    public class SamsungValidation : Validation
    {
        // TODO
        // flush out with correct response
        // appears to have a paymentId(transactionid), itemId(offerid) based on old version
        
        internal const string DB_KEY_STATUS = "status";

        public const string FRIENDLY_KEY_STATUS = "status";
        
        [BsonElement(DB_KEY_STATUS)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
        public string Status { get; private set; }

        public SamsungValidation(string status)
        {
            Status = status;
        }
    }
}

/*
status:'redeemed',
message:"Receipt is already redeemed",
response:samsungResponse,
offerId: samsungResponse.itemId ?: ""

status       : 'success',
response     : samsungResponse,
transactionId: samsungResponse.paymentId,
offerId      : samsungResponse.itemId,
receiptKey   : receiptKey,
receiptData  : "${samsungResponse as JSON}".toString(),
ts           : System.currentTimeMillis() as String //Documentation is private
*/
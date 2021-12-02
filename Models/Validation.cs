using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService.Models
{
    public abstract class Validation : PlatformDataModel
    {
        internal const string DB_KEY_STATUS = "status";
        internal const string DB_KEY_RESPONSE = "resp";
        internal const string DB_KEY_TRANSACTION_ID = "tid";
        internal const string DB_KEY_OFFER_ID = "oid";
        internal const string DB_KEY_RECEIPT_KEY = "rcptKey";
        internal const string DB_KEY_RECEIPT_DATA = "rcptData";
        internal const string DB_KEY_TIMESTAMP = "tmestmp";

        public const string FRIENDLY_KEY_STATUS = "status";
        public const string FRIENDLY_KEY_RESPONSE = "response";
        public const string FRIENDLY_KEY_TRANSACTION_ID = "transactionId";
        public const string FRIENDLY_KEY_OFFER_ID = "offerId";
        public const string FRIENDLY_KEY_RECEIPT_KEY = "receiptKey";
        public const string FRIENDLY_KEY_RECEIPT_DATA = "receiptData";
        public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
        
        [BsonElement(DB_KEY_STATUS)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
        public string Status { get; private set; }
        
        [BsonElement(DB_KEY_RESPONSE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RESPONSE)]
        public Receipt Response { get; private set; }

        [BsonElement(DB_KEY_TRANSACTION_ID)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_ID)]
        public string TransactionId { get; private set; }

        [BsonElement(DB_KEY_OFFER_ID)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_ID)]
        public string OfferId { get; private set; }

        [BsonElement(DB_KEY_RECEIPT_KEY)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_KEY)]
        public string ReceiptKey { get; private set; }

        [BsonElement(DB_KEY_RECEIPT_DATA)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_DATA)]
        public string ReceiptData { get; private set; }

        [BsonElement(DB_KEY_TIMESTAMP)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
        public long Timestamp { get; private set; }

        public Validation(string status, Receipt response, string transactionId, string offerId, string receiptKey,
            string receiptData, long timestamp)
        {
            Status = status;
            Response = response;
            TransactionId = transactionId;
            OfferId = offerId;
            ReceiptKey = receiptKey;
            ReceiptData = receiptData;
            Timestamp = timestamp;
        }
    }
}

// other validations build off this
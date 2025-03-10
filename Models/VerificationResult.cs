using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities.JsonTools;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.ReceiptService.Models;

public class VerificationResult : PlatformDataModel
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
    public SuccessStatus Status { get; set; }
    
    [BsonElement(DB_KEY_RESPONSE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RESPONSE)]
    public Receipt Response { get; set; }

    [BsonElement(DB_KEY_TRANSACTION_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_ID)]
    public string TransactionId { get; set; }

    [BsonElement(DB_KEY_OFFER_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_ID)]
    public string OfferId { get; set; }

    [BsonElement(DB_KEY_RECEIPT_KEY)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_KEY)]
    public string ReceiptKey { get; set; }

    [BsonElement(DB_KEY_RECEIPT_DATA)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_DATA)]
    public string ReceiptData { get; set; }

    [BsonElement(DB_KEY_TIMESTAMP)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
    public long Timestamp { get; set; }
    
    public VerificationResult(){}

    public VerificationResult WithStatus(SuccessStatus status)
    {
        Status = status;
        return this;
    }
}
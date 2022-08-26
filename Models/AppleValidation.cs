using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.ReceiptService.Models;

public class AppleValidation : Validation
{
    // response from apple
    // string environment (Production, Sandbox)
    // boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
    // byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
    // list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
    // list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
    // json receipt (json) of receipt sent for verification
    // int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes

    internal const string DB_KEY_ENVIRONMENT = "env";
    internal const string DB_KEY_IS_RETRYABLE = "isRetry";
    internal const string DB_KEY_LATEST_RECEIPT = "latestRcpt";
    internal const string DB_KEY_LATEST_RECEIPT_INFO = "latestRcptInf";
    internal const string DB_KEY_PENDING_RENEWAL_INFO = "pndRnwInf";
    internal const string DB_KEY_RECEIPT = "rcpt";
    internal const string DB_KEY_STATUS = "status";

    public const string FRIENDLY_KEY_ENVIRONMENT = "environment";
    public const string FRIENDLY_KEY_IS_RETRYABLE = "isRetryable";
    public const string FRIENDLY_KEY_LATEST_RECEIPT = "latestReceipt";
    public const string FRIENDLY_KEY_LATEST_RECEIPT_INFO = "latestReceiptInfo";
    public const string FRIENDLY_KEY_PENDING_RENEWAL_INFO = "pendingRenewalInfo";
    public const string FRIENDLY_KEY_RECEIPT = "receipt";
    public const string FRIENDLY_KEY_STATUS = "status";
    
    [BsonElement(DB_KEY_ENVIRONMENT)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENVIRONMENT)]
    public string Environment { get; set; }
    
    [BsonElement(DB_KEY_IS_RETRYABLE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_RETRYABLE)]
    public int IsRetryable { get; set; } // 0 (no) or 1 (yes) boolean
    
    [BsonElement(DB_KEY_LATEST_RECEIPT), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_LATEST_RECEIPT)]
    public byte? LatestReceipt { get; set; } // base64 encoded, only for auto-renewable
    
    [BsonElement(DB_KEY_LATEST_RECEIPT_INFO), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_LATEST_RECEIPT_INFO)]
    public List<string> LatestReceiptInfo { get; set; } // assuming strings for now, only for auto-renewable
    
    [BsonElement(DB_KEY_PENDING_RENEWAL_INFO), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PENDING_RENEWAL_INFO)]
    public List<string> PendingRenewalInfo { get; set; } // assuming strings for now, only for auto-renewable
    
    [BsonElement(DB_KEY_RECEIPT)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT)]
    public Receipt Receipt { get; set; } // may have to make sure the format works for apple
    
    [BsonElement(DB_KEY_STATUS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
    public int Status { get; set; } // 0 if valid, status code otherwise
}

/*
status: 'redeemed',
message: "Receipt is already redeemed",
offerId: it['product_id'] ?: ""

status       : 'success',
response     : itunesResponse.receipt,
transactionId: transactionId,
offerId      : it['product_id'],
receiptKey   : itunesReceiptKey,
receiptData  : it.toString(),
ts           : it.purchase_date_ms ?: System.currentTimeMillis()
*/

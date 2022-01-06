using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.ReceiptService.Models
{
    public class GoogleValidation : Validation
    {
        /*
        will require -only- the following:
        "orderId": "your.order.id",
        "packageName": "your.package.name",
        "productId": "your.product.id",
        "purchaseTime": 1526476218113,
        "purchaseState": 0,
        "purchaseToken": "your.purchase.token"
        */

        internal const string DB_KEY_ORDER_ID = "oid";
        internal const string DB_KEY_PACKAGE_NAME = "pkgNm";
        internal const string DB_KEY_PRODUCT_ID = "prodId";
        internal const string DB_KEY_PURCHASE_TIME = "purchTime";
        internal const string DB_KEY_PURCHASE_STATE = "purchState";
        internal const string DB_KEY_PURCHASE_TOKEN = "purchTkn";
        internal const string DB_KEY_ACKNOWLEDGED = "acklged";

        public const string FRIENDLY_KEY_ORDER_ID = "orderID";
        public const string FRIENDLY_KEY_PACKAGE_NAME = "packageName";
        public const string FRIENDLY_KEY_PRODUCT_ID = "productId";
        public const string FRIENDLY_KEY_PURCHASE_TIME = "purchaseTime";
        public const string FRIENDLY_KEY_PURCHASE_STATE = "purchaseState";
        public const string FRIENDLY_KEY_PURCHASE_TOKEN = "purchaseToken";
        public const string FRIENDLY_KEY_ACKNOWLEDGED = "acknowledged";
        
        [BsonElement(DB_KEY_ORDER_ID)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORDER_ID)]
        public string OrderId { get; private set; }

        [BsonElement(DB_KEY_PACKAGE_NAME)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PACKAGE_NAME)]
        public string PackageName { get; private set; }
        
        [BsonElement(DB_KEY_PRODUCT_ID)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
        public string ProductId { get; private set; }
        
        [BsonElement(DB_KEY_PURCHASE_TIME)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_TIME)]
        public long PurchaseTime { get; private set; }
        
        [BsonElement(DB_KEY_PURCHASE_STATE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_STATE)]
        public int PurchaseState { get; private set; }
        
        [BsonElement(DB_KEY_PURCHASE_TOKEN)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_TOKEN)]
        public string PurchaseToken { get; private set; }
        
        [BsonElement(DB_KEY_ACKNOWLEDGED)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACKNOWLEDGED)]
        public bool Acknowledged { get; private set; }

        public GoogleValidation(string orderId, string packageName, string productId, long purchaseTime,
            int purchaseState, string purchaseToken, bool acknowledged)
        {
            OrderId = orderId;
            PackageName = packageName;
            ProductId = productId;
            PurchaseTime = purchaseTime;
            PurchaseState = purchaseState;
            PurchaseToken = purchaseToken;
            Acknowledged = acknowledged;
        }
    }
}

// don't actually need an external service
/*
status:'redeemed',
message:"Receipt is already redeemed",
offerId: receipt.productId ?: ""

status       : 'success',
response     : receipt,
transactionId: transactionId,
offerId      : receipt.productId,
receiptKey   : receiptKey,
receiptData  : sreceipt,
ts           : receipt.purchaseTime as String ?: System.currentTimeMillis()
*/
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;

namespace Rumble.Platform.ReceiptService.Models
{
    public class Receipt : PlatformCollectionDocument
    {
        internal const string DB_KEY_ORDER_ID = "orderId";
        internal const string DB_KEY_PACKAGE_NAME = "pkgName";
        internal const string DB_KEY_PRODUCT_ID = "prodId";
        internal const string DB_KEY_PURCHASE_TIME = "purchTme";
        internal const string DB_KEY_PURCHASE_STATE = "purchState";
        internal const string DB_KEY_PURCHASE_TOKEN = "purchTkn";
        internal const string DB_KEY_ACKNOWLEDGED = "acknlged";

        public const string FRIENDLY_KEY_ORDER_ID = "orderId";
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

        public Receipt(string orderId, string packageName, string productId, long purchaseTime, int purchaseState,
            string purchaseToken, bool acknowledged)
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
// Receipt
// - orderId (string)
// - packageName (string)
// - productId (string)
// - purchaseTime (unixtime) redis currently uses unixtime in ms
// - purchaseState (0, 1)
// - purchaseToken (string)
// - acknowledged (true, false) tbh don't know what this is, but it's present in redis with value: false; it's not in the documentation
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;

namespace Rumble.Platform.ReceiptService.Models;

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
    public string OrderId { get; set; }
    
    [BsonElement(DB_KEY_PACKAGE_NAME)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PACKAGE_NAME)]
    public string PackageName { get; set; }
    
    [BsonElement(DB_KEY_PRODUCT_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
    public string ProductId { get; set; }
    
    [BsonElement(DB_KEY_PURCHASE_TIME)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_TIME)]
    public long PurchaseTime { get; set; }
    
    [BsonElement(DB_KEY_PURCHASE_STATE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_STATE)]
    public int PurchaseState { get; set; }
    
    [BsonElement(DB_KEY_PURCHASE_TOKEN)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_TOKEN)]
    public string PurchaseToken { get; set; }
    
    [BsonElement(DB_KEY_ACKNOWLEDGED)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACKNOWLEDGED)]
    public bool Acknowledged { get; set; }

    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (OrderId == null)
            errors.Add("OrderId cannot be null.");
        if (ProductId == null)
            errors.Add("ProductId cannot be null.");
        // if (Signature == null)
        //     errors.Add("Signature cannot be null.");
        
        
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
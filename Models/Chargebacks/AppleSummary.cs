using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class AppleSummary : PlatformDataModel
{
	internal const string DB_KEY_REQUEST_IDENTIFIER       = "rqId";
	internal const string DB_KEY_ENVIRONMENT              = "env";
	internal const string DB_KEY_APP_APPLE_ID             = "appId";
	internal const string DB_KEY_BUNDLE_ID                = "bunId";
	internal const string DB_KEY_PRODUCT_ID               = "prodId";
	internal const string DB_KEY_STOREFRONT_COUNTRY_CODES = "sfCntryCds";
	internal const string DB_KEY_FAILED_COUNT             = "failCnt";
	internal const string DB_KEY_SUCCEEDED_COUNT          = "succCnt";

	public const string FRIENDLY_KEY_REQUEST_IDENTIFIER       = "requestIdentifier";
	public const string FRIENDLY_KEY_ENVIRONMENT              = "environment";
	public const string FRIENDLY_KEY_APP_APPLE_ID             = "appAppleId";
	public const string FRIENDLY_KEY_BUNDLE_ID                = "bundleId";
	public const string FRIENDLY_KEY_PRODUCT_ID               = "productId";
	public const string FRIENDLY_KEY_STOREFRONT_COUNTRY_CODES = "storefrontCountryCodes";
	public const string FRIENDLY_KEY_FAILED_COUNT             = "failedCount";
	public const string FRIENDLY_KEY_SUCCEEDED_COUNT          = "SucceededCount";
	
	[BsonElement(DB_KEY_REQUEST_IDENTIFIER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REQUEST_IDENTIFIER)]
	public string RequestIdentifier { get; set; }
	
	[BsonElement(DB_KEY_ENVIRONMENT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENVIRONMENT)]
	public string Environment { get; set; }
	
	[BsonElement(DB_KEY_APP_APPLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_APP_APPLE_ID)]
	public string AppAppleId { get; set; } // int64 on apple's side
	
	[BsonElement(DB_KEY_BUNDLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BUNDLE_ID)]
	public string BundleId { get; set; }
	
	[BsonElement(DB_KEY_PRODUCT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
	public string ProductId { get; set; }

	[BsonElement(DB_KEY_STOREFRONT_COUNTRY_CODES)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_STOREFRONT_COUNTRY_CODES)]
	public string StorefrontCountryCodes { get; set; }
	
	[BsonElement(DB_KEY_FAILED_COUNT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FAILED_COUNT)]
	public long FailedCount { get; set; }
	
	[BsonElement(DB_KEY_SUCCEEDED_COUNT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUCCEEDED_COUNT)]
	public long SucceededCount { get; set; }
}
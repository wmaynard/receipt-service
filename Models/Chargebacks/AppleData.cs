using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class AppleData : PlatformDataModel
{
	internal const string DB_KEY_APP_APPLE_ID            = "appId";
	internal const string DB_KEY_BUNDLE_ID               = "bunId";
	internal const string DB_KEY_BUNDLE_VERSION          = "bunVer";
	internal const string DB_KEY_ENVIRONMENT             = "env";
	internal const string DB_KEY_SIGNED_RENEWAL_INFO     = "sgnRenewInfo";
	internal const string DB_KEY_SIGNED_TRANSACTION_INFO = "sgnTransInfo";
	internal const string DB_KEY_STATUS                  = "status";

	public const string FRIENDLY_KEY_APP_APPLE_ID            = "appAppleId";
	public const string FRIENDLY_KEY_BUNDLE_ID               = "bundleId";
	public const string FRIENDLY_KEY_BUNDLE_VERSION          = "bundleVersion";
	public const string FRIENDLY_KEY_ENVIRONMENT             = "environment";
	public const string FRIENDLY_KEY_SIGNED_RENEWAL_INFO     = "signedRenewalInfo";
	public const string FRIENDLY_KEY_SIGNED_TRANSACTION_INFO = "signedTransactionInfo";
	public const string FRIENDLY_KEY_STATUS                  = "status";
	
	[BsonElement(DB_KEY_APP_APPLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_APP_APPLE_ID)]
	public long AppAppleId { get; set; } // int64 on apple's side
	
	[BsonElement(DB_KEY_BUNDLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BUNDLE_ID)]
	public string BundleId { get; set; }
	
	[BsonElement(DB_KEY_BUNDLE_VERSION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BUNDLE_VERSION)]
	public string BundleVersion { get; set; }
	
	[BsonElement(DB_KEY_ENVIRONMENT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENVIRONMENT)]
	public string Environment { get; set; }
	
	[BsonElement(DB_KEY_SIGNED_RENEWAL_INFO)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SIGNED_RENEWAL_INFO)]
	public string JWSRenewalInfo { get; set; } // needs to be base64 url decoded
	
	[BsonElement(DB_KEY_SIGNED_TRANSACTION_INFO)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SIGNED_TRANSACTION_INFO)]
	public string JWSTransaction { get; set; } // needs to be base64 url decoded
	
	[BsonElement(DB_KEY_STATUS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
	public string Status { get; set; }
}
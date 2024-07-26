using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class AppleTransactionInfo : PlatformDataModel
{
	internal const string DB_KEY_APP_ACCOUNT_TOKEN             = "accTkn";
	internal const string DB_KEY_BUNDLE_ID                     = "bunId";
	internal const string DB_KEY_ENVIRONMENT                   = "env";
	internal const string DB_KEY_EXPIRES_DATE                  = "expDate";
	internal const string DB_KEY_IN_APP_OWNERSHIP_TYPE         = "iaOwnType";
	internal const string DB_KEY_IS_UPGRADED                   = "upgraded";
	internal const string DB_KEY_OFFER_IDENTIFIER              = "offId";
	internal const string DB_KEY_OFFER_TYPE                    = "offType";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE        = "origPurchDate";
	internal const string DB_KEY_ORIGINAL_TRANSACTION_ID       = "origTransId";
	internal const string DB_KEY_PRODUCT_ID                    = "prodId";
	internal const string DB_KEY_PURCHASE_DATE                 = "purchDate";
	internal const string DB_KEY_QUANTITY                      = "qty";
	internal const string DB_KEY_REVOCATION_DATE               = "rvcDate";
	internal const string DB_KEY_REVOCATION_REASON             = "rvcRsn";
	internal const string DB_KEY_SIGNED_DATE                   = "sgnDate";
	internal const string DB_KEY_STOREFRONT                    = "sf";
	internal const string DB_KEY_STOREFRONT_ID                 = "sfId";
	internal const string DB_KEY_SUBSCRIPTION_GROUP_IDENTIFIER = "subGrpId";
	internal const string DB_KEY_TRANSACTION_ID                = "transId";
	internal const string DB_KEY_TRANSACTION_REASON            = "transRsn";
	internal const string DB_KEY_TYPE                          = "type";
	internal const string DB_KEY_WEB_ORDER_LINE_ITEM_ID        = "webOrderLineItemId";

	public const string FRIENDLY_KEY_APP_ACCOUNT_TOKEN             = "appAccountToken";
	public const string FRIENDLY_KEY_BUNDLE_ID                     = "bundleId";
	public const string FRIENDLY_KEY_ENVIRONMENT                   = "environment";
	public const string FRIENDLY_KEY_EXPIRES_DATE                  = "expiresDate";
	public const string FRIENDLY_KEY_IN_APP_OWNERSHIP_TYPE         = "inAppOwnershipType";
	public const string FRIENDLY_KEY_IS_UPGRADED                   = "isUpgraded";
	public const string FRIENDLY_KEY_OFFER_IDENTIFIER              = "offerIdentifier";
	public const string FRIENDLY_KEY_OFFER_TYPE                    = "offerType";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE        = "originalPurchaseDate";
	public const string FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID       = "originalTransactionId";
	public const string FRIENDLY_KEY_PRODUCT_ID                    = "productId";
	public const string FRIENDLY_KEY_PURCHASE_DATE                 = "purchaseDate";
	public const string FRIENDLY_KEY_QUANTITY                      = "quantity";
	public const string FRIENDLY_KEY_REVOCATION_DATE               = "revocationDate";
	public const string FRIENDLY_KEY_REVOCATION_REASON             = "revocationReason";
	public const string FRIENDLY_KEY_SIGNED_DATE                   = "signedDate";
	public const string FRIENDLY_KEY_STOREFRONT                    = "storefront";
	public const string FRIENDLY_KEY_STOREFRONT_ID                 = "storefrontId";
	public const string FRIENDLY_KEY_SUBSCRIPTION_GROUP_IDENTIFIER = "subscriptionGroupIdentifier";
	public const string FRIENDLY_KEY_TRANSACTION_ID                = "transactionId";
	public const string FRIENDLY_KEY_TRANSACTION_REASON            = "transactionReason";
	public const string FRIENDLY_KEY_TYPE                          = "type";
	public const string FRIENDLY_KEY_WEB_ORDER_LINE_ITEM_ID        = "webOrderLineItemId";

	[BsonElement(DB_KEY_APP_ACCOUNT_TOKEN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_APP_ACCOUNT_TOKEN)]
	public string AppAccountToken { get; set; }
	
	[BsonElement(DB_KEY_BUNDLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BUNDLE_ID)]
	public string BundleId { get; set; }
	
	[BsonElement(DB_KEY_ENVIRONMENT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENVIRONMENT)]
	public string Environment { get; set; }
	
	[BsonElement(DB_KEY_EXPIRES_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRES_DATE)]
	public long ExpiresDate { get; set; }
	
	[BsonElement(DB_KEY_IN_APP_OWNERSHIP_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IN_APP_OWNERSHIP_TYPE)]
	public string InAppOwnershipType { get; set; }
	
	[BsonElement(DB_KEY_IS_UPGRADED)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_UPGRADED)]
	public bool IsUpgraded { get; set; }

	[BsonElement(DB_KEY_OFFER_IDENTIFIER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_IDENTIFIER)]
	public string OfferIdentifier { get; set; } // only when offertype is 2 or 3
	
	[BsonElement(DB_KEY_OFFER_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_TYPE)]
	public int OfferType { get; set; } //  1: introductory, 2: promo, 3: subscription offer code
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE)]
	public long OriginalPurchaseDate { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID)]
	public string OriginalTransactionId { get; set; }
	
	[BsonElement(DB_KEY_PRODUCT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
	public string ProductId { get; set; }
	
	[BsonElement(DB_KEY_PURCHASE_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_DATE)]
	public long PurchaseDate { get; set; }
	
	[BsonElement(DB_KEY_QUANTITY)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_QUANTITY)]
	public int Quantity { get; set; }
	
	[BsonElement(DB_KEY_REVOCATION_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REVOCATION_DATE)]
	public long RevocationDate { get; set; }

	public enum RevocationReasons
	{
		Unknown,
		IssueOnApp,
		Other
	}
	[BsonElement(DB_KEY_REVOCATION_REASON)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REVOCATION_REASON)]
	public RevocationReasons RevocationReason { get; set; } // 1: issue on app, 2: other reasons

	[BsonElement(DB_KEY_SIGNED_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SIGNED_DATE)]
	public long SignedDate { get; set; }
	
	[BsonElement(DB_KEY_STOREFRONT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_STOREFRONT)]
	public string Storefront { get; set; }
	
	[BsonElement(DB_KEY_STOREFRONT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_STOREFRONT_ID)]
	public string StorefrontId { get; set; }
	
	[BsonElement(DB_KEY_SUBSCRIPTION_GROUP_IDENTIFIER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUBSCRIPTION_GROUP_IDENTIFIER)]
	public string SubscriptionGroupIdentifier { get; set; }
	
	[BsonElement(DB_KEY_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_ID)]
	public string TransactionId { get; set; }
	
	[BsonElement(DB_KEY_TRANSACTION_REASON)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_REASON)]
	public string TransactionReason { get; set; }
	
	[BsonElement(DB_KEY_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TYPE)]
	public string Type { get; set; }
	
	[BsonElement(DB_KEY_WEB_ORDER_LINE_ITEM_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_WEB_ORDER_LINE_ITEM_ID)]
	public string WebOrderLineItemId { get; set; }
}
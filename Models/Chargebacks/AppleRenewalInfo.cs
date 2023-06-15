using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class AppleRenewalInfo : PlatformDataModel
{
	internal const string DB_KEY_AUTO_RENEW_PRODUCT_ID = "autoReProdId";
	internal const string DB_KEY_AUTO_RENEW_STATUS = "autoReSts";
	internal const string DB_KEY_ENVIRONMENT = "env";
	internal const string DB_KEY_EXPIRATION_INTENT = "expInt";
	internal const string DB_KEY_GRACE_PERIOD_EXPIRES_DATE = "gracePd";
	internal const string DB_KEY_IS_IN_BILLING_RETRY_PERIOD = "retryPd";
	internal const string DB_KEY_OFFER_IDENTIFIER = "offerId";
	internal const string DB_KEY_OFFER_TYPE = "offerType";
	internal const string DB_KEY_ORIGINAL_TRANSACTION_ID = "origTransId";
	internal const string DB_KEY_PRICE_INCREASE_STATUS = "priceIncSts";
	internal const string DB_KEY_PRODUCT_ID = "prodId";
	internal const string DB_KEY_RECENT_SUBSCRIPTION_START_DATE = "recSubStart";
	internal const string DB_KEY_RENEWAL_DATE = "renDate";
	internal const string DB_KEY_SIGNED_DATE = "sgnDate";

	public const string FRIENDLY_KEY_AUTO_RENEW_PRODUCT_ID = "autoRenewProductId";
	public const string FRIENDLY_KEY_AUTO_RENEW_STATUS = "autoRenewStatus";
	public const string FRIENDLY_KEY_ENVIRONMENT = "environment";
	public const string FRIENDLY_KEY_EXPIRATION_INTENT = "expirationIntent";
	public const string FRIENDLY_KEY_GRACE_PERIOD_EXPIRES_DATE = "gracePeriodExpiresDate";
	public const string FRIENDLY_KEY_IS_IN_BILLING_RETRY_PERIOD = "isInBillingRetryPeriod";
	public const string FRIENDLY_KEY_OFFER_IDENTIFIER = "offerIdentifier";
	public const string FRIENDLY_KEY_OFFER_TYPE = "offerType";
	public const string FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID = "originalTransactionId";
	public const string FRIENDLY_KEY_PRICE_INCREASE_STATUS = "priceIncreaseStatus";
	public const string FRIENDLY_KEY_PRODUCT_ID = "productId";
	public const string FRIENDLY_KEY_RECENT_SUBSCRIPTION_START_DATE = "recentSubscriptionStartDate";
	public const string FRIENDLY_KEY_RENEWAL_DATE = "renewalDate";
	public const string FRIENDLY_KEY_SIGNED_DATE = "signedDate";

	[BsonElement(DB_KEY_AUTO_RENEW_PRODUCT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_AUTO_RENEW_PRODUCT_ID)]
	public string AutoRenewProductId { get; set; }
	
	[BsonElement(DB_KEY_AUTO_RENEW_STATUS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_AUTO_RENEW_STATUS)]
	public int AutoRenewStatus { get; set; } // 0: off, 1: on
	
	[BsonElement(DB_KEY_ENVIRONMENT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ENVIRONMENT)]
	public string Environment { get; set; }
	
	[BsonElement(DB_KEY_EXPIRATION_INTENT)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRATION_INTENT)]
	public int ExpirationIntent { get; set; } // 1: canceled, 2: billing error, 3: no consent to price increase, 4: product unavailable, 5: other
	
	[BsonElement(DB_KEY_GRACE_PERIOD_EXPIRES_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_GRACE_PERIOD_EXPIRES_DATE)]
	public long GracePeriodExpiresDate { get; set; }
	
	[BsonElement(DB_KEY_IS_IN_BILLING_RETRY_PERIOD)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_IN_BILLING_RETRY_PERIOD)]
	public bool IsInBillingRetryPeriod { get; set; }

	[BsonElement(DB_KEY_OFFER_IDENTIFIER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_IDENTIFIER)]
	public string OfferIdentifier { get; set; } // only when offertype is 2 or 3
	
	[BsonElement(DB_KEY_OFFER_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_OFFER_TYPE)]
	public int OfferType { get; set; } //  1: introductory, 2: promo, 3: subscription offer code
	
	[BsonElement(DB_KEY_ORIGINAL_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID)]
	public string OriginalTransactionId { get; set; }
	
	[BsonElement(DB_KEY_PRICE_INCREASE_STATUS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRICE_INCREASE_STATUS)]
	public int PriceIncreaseStatus { get; set; } // 0: no response, 1: consent or notified that consent not required
	
	[BsonElement(DB_KEY_PRODUCT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
	public string ProductId { get; set; }
	
	[BsonElement(DB_KEY_RECENT_SUBSCRIPTION_START_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECENT_SUBSCRIPTION_START_DATE)]
	public long RecentSubscriptionStartDate { get; set; }
	
	[BsonElement(DB_KEY_RENEWAL_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RENEWAL_DATE)]
	public long RenewalDate { get; set; }
	
	[BsonElement(DB_KEY_SIGNED_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SIGNED_DATE)]
	public long SignedDate { get; set; }
}
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.ReceiptService.Models;

[BsonIgnoreExtraElements]
public class AppleInApp : PlatformDataModel
{
	internal const string DB_KEY_QUANTITY                   = "qty";
	internal const string DB_KEY_PRODUCT_ID                 = "prodTd";
	internal const string DB_KEY_TRANSACTION_ID             = "tranId";
	internal const string DB_KEY_ORIGINAL_TRANSACTION_ID    = "origTranId";
	internal const string DB_KEY_PURCHASE_DATE              = "purDate";
	internal const string DB_KEY_PURCHASE_DATE_MS           = "purDateMs";
	internal const string DB_KEY_PURCHASE_DATE_PST          = "purDatePst";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE     = "origPurDate";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE_MS  = "origPurDateMs";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE_PST = "origPurDatePst";
	internal const string DB_KEY_IS_TRIAL_PERIOD            = "isTrial";
	internal const string DB_KEY_IN_APP_OWNERSHIP_TYPE      = "inAppOwnType";
	
	public const string FRIENDLY_KEY_QUANTITY                   = "quantity";
	public const string FRIENDLY_KEY_PRODUCT_ID                 = "productId";
	public const string FRIENDLY_KEY_TRANSACTION_ID             = "transactionId";
	public const string FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID    = "originalTransactionId";
	public const string FRIENDLY_KEY_PURCHASE_DATE              = "purchaseDate";
	public const string FRIENDLY_KEY_PURCHASE_DATE_MS           = "purchaseDateMs";
	public const string FRIENDLY_KEY_PURCHASE_DATE_PST          = "purchaseDatePst";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE     = "originalPurchaseDate";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_MS  = "originalPurchaseDateMs";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_PST = "originalPurchaseDatePst";
	public const string FRIENDLY_KEY_IS_TRIAL_PERIOD            = "isTrialPeriod";
	public const string FRIENDLY_KEY_IN_APP_OWNERSHIP_TYPE      = "inAppOwnershipType";
	
	[BsonElement(DB_KEY_QUANTITY)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_QUANTITY)]
	public string Quantity { get; set; }
	
	[BsonElement(DB_KEY_PRODUCT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PRODUCT_ID)]
	public string ProductId { get; set; }
	
	[BsonElement(DB_KEY_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_ID)]
	public string TransactionId { get; set; }

	[BsonElement(DB_KEY_ORIGINAL_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_TRANSACTION_ID)]
	public string OriginalTransactionId { get; set; }

	[BsonElement(DB_KEY_PURCHASE_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_DATE)]
	public string PurchaseDate { get; set; }
	
	[BsonElement(DB_KEY_PURCHASE_DATE_MS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_DATE_MS)]
	public string PurchaseDateMs { get; set; }
	
	[BsonElement(DB_KEY_PURCHASE_DATE_PST)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_PURCHASE_DATE_PST)]
	public string PurchaseDatePst { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE)]
	public string OriginalPurchaseDate { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE_MS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_MS)]
	public string OriginalPurchaseDateMs { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE_PST)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_PST)]
	public string OriginalPurchaseDatePst { get; set; }
	
	[BsonElement(DB_KEY_IS_TRIAL_PERIOD)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_TRIAL_PERIOD)]
	public string IsTrialPeriod { get; set; } // not bool
	
	[BsonElement(DB_KEY_IN_APP_OWNERSHIP_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IN_APP_OWNERSHIP_TYPE)]
	public string InAppOwnershipType { get; set; }
}
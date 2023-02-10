using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.ReceiptService.Models;

[BsonIgnoreExtraElements]
public class AppleReceipt : Receipt
{
	internal const string DB_KEY_RECEIPT_TYPE                 = "rcptType";
	internal const string DB_KEY_ADAM_ID                      = "adamTd";
	internal const string DB_KEY_APP_ITEM_ID                  = "appItId";
	internal const string DB_KEY_BUNDLE_ID                    = "bunId";
	internal const string DB_KEY_APPLICATION_VERSION          = "appVer";
	internal const string DB_KEY_DOWNLOAD_ID                  = "dwnId";
	internal const string DB_KEY_VERSION_EXTERNAL_IDENTIFIER  = "verExtId";
	internal const string DB_KEY_RECEIPT_CREATION_DATE        = "rcptDate";
	internal const string DB_KEY_RECEIPT_CREATION_DATE_MS     = "rcptDateMs";
	internal const string DB_KEY_RECEIPT_CREATION_DATE_PST    = "rcptDatePst";
	internal const string DB_KEY_REQUEST_DATE                 = "reqDate";
	internal const string DB_KEY_REQUEST_DATE_MS              = "reqDateMs";
	internal const string DB_KEY_REQUEST_DATE_PST             = "reqDatePst";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE       = "origPurDate";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE_MS    = "origPurDateMs";
	internal const string DB_KEY_ORIGINAL_PURCHASE_DATE_PST   = "origPurDatePst";
	internal const string DB_KEY_ORIGINAL_APPLICATION_VERSION = "origAppVer";
	internal const string DB_KEY_IN_APP                       = "inApp";
	
	public const string FRIENDLY_KEY_RECEIPT_TYPE                 = "receipt_type";
	public const string FRIENDLY_KEY_ADAM_ID                      = "adam_id";
	public const string FRIENDLY_KEY_APP_ITEM_ID                  = "app_item_id";
	public const string FRIENDLY_KEY_BUNDLE_ID                    = "bundle_id";
	public const string FRIENDLY_KEY_APPLICATION_VERSION          = "application_version";
	public const string FRIENDLY_KEY_DOWNLOAD_ID                  = "download_id";
	public const string FRIENDLY_KEY_VERSION_EXTERNAL_IDENTIFIER  = "version_external_identifier";
	public const string FRIENDLY_KEY_RECEIPT_CREATION_DATE        = "receipt_creation_date";
	public const string FRIENDLY_KEY_RECEIPT_CREATION_DATE_MS     = "receipt_creation_date_ms";
	public const string FRIENDLY_KEY_RECEIPT_CREATION_DATE_PST    = "receipt_creation_date_pst";
	public const string FRIENDLY_KEY_REQUEST_DATE                 = "request_date";
	public const string FRIENDLY_KEY_REQUEST_DATE_MS              = "request_date_ms";
	public const string FRIENDLY_KEY_REQUEST_DATE_PST             = "request_date_pst";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE       = "original_purchase_date";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_MS    = "original_purchase_date_ms";
	public const string FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_PST   = "original_purchase_date_pst";
	public const string FRIENDLY_KEY_ORIGINAL_APPLICATION_VERSION = "original_application_version";
	public const string FRIENDLY_KEY_IN_APP                       = "in_app";
	
	[BsonElement(DB_KEY_RECEIPT_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_TYPE)]
	public string ReceiptType { get; set; }
	
	[BsonElement(DB_KEY_ADAM_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ADAM_ID)]
	public long AdamId { get; set; }
	
	[BsonElement(DB_KEY_APP_ITEM_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_APP_ITEM_ID)]
	public long AppItemId { get; set; }

	[BsonElement(DB_KEY_BUNDLE_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BUNDLE_ID)]
	public string BundleId { get; set; }

	[BsonElement(DB_KEY_APPLICATION_VERSION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_APPLICATION_VERSION)]
	public string ApplicationVersion { get; set; }
	
	[BsonElement(DB_KEY_DOWNLOAD_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_DOWNLOAD_ID)]
	public long DownloadId { get; set; }
	
	[BsonElement(DB_KEY_VERSION_EXTERNAL_IDENTIFIER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_VERSION_EXTERNAL_IDENTIFIER)]
	public long VersionExternalIdentifier { get; set; }
	
	[BsonElement(DB_KEY_RECEIPT_CREATION_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_CREATION_DATE)]
	public string ReceiptCreationDate { get; set; }
	
	[BsonElement(DB_KEY_RECEIPT_CREATION_DATE_MS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_CREATION_DATE_MS)]
	public string ReceiptCreationDateMs { get; set; }
	
	[BsonElement(DB_KEY_RECEIPT_CREATION_DATE_PST)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECEIPT_CREATION_DATE_PST)]
	public string ReceiptCreationDatePst { get; set; }
	
	[BsonElement(DB_KEY_REQUEST_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REQUEST_DATE)]
	public string RequestDate { get; set; }
	
	[BsonElement(DB_KEY_REQUEST_DATE_MS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REQUEST_DATE_MS)]
	public string RequestDateMs { get; set; }
	
	[BsonElement(DB_KEY_REQUEST_DATE_PST)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REQUEST_DATE_PST)]
	public string RequestDatePst { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE)]
	public string OriginalPurchaseDate { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE_MS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_MS)]
	public string OriginalPurchaseDateMs { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_PURCHASE_DATE_PST)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_PURCHASE_DATE_PST)]
	public string OriginalPurchaseDatePst { get; set; }
	
	[BsonElement(DB_KEY_ORIGINAL_APPLICATION_VERSION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGINAL_APPLICATION_VERSION)]
	public string OriginalApplicationVersion { get; set; }
	
	[BsonElement(DB_KEY_IN_APP)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IN_APP)]
	public List<AppleInApp> InApp { get; set; }
}
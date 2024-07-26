using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class AppleChargeback : PlatformDataModel
{
	internal const string DB_KEY_NOTIFICATION_TYPE = "notifType";
	internal const string DB_KEY_SUBTYPE           = "subType";
	internal const string DB_KEY_DATA              = "data";
	internal const string DB_KEY_SUMMARY           = "summ";
	internal const string DB_KEY_VERSION           = "ver";
	internal const string DB_KEY_SIGNED_DATE       = "sgnDate";
	internal const string DB_KEY_NOTIFICATION_UUID = "notifUUID";

	public const string FRIENDLY_KEY_NOTIFICATION_TYPE = "notificationType";
	public const string FRIENDLY_KEY_SUBTYPE           = "subType";
	public const string FRIENDLY_KEY_DATA              = "data";
	public const string FRIENDLY_KEY_SUMMARY           = "summary";
	public const string FRIENDLY_KEY_VERSION           = "version";
	public const string FRIENDLY_KEY_SIGNED_DATE       = "signedDate";
	public const string FRIENDLY_KEY_NOTIFICATION_UUID = "notificationUUID";
	
	[BsonElement(DB_KEY_NOTIFICATION_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NOTIFICATION_TYPE)]
	public string NotificationType { get; set; }
	
	[BsonElement(DB_KEY_SUBTYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUBTYPE)]
	public string SubType { get; set; }
	
	[BsonElement(DB_KEY_DATA)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_DATA)]
	public AppleData Data { get; set; }
	
	[BsonElement(DB_KEY_SUMMARY)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUMMARY)]
	public AppleSummary Summary { get; set; }
	
	[BsonElement(DB_KEY_VERSION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_VERSION)]
	public string Version { get; set; }
	
	[BsonElement(DB_KEY_SIGNED_DATE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SIGNED_DATE)]
	public long SignedDate { get; set; }
	
	[BsonElement(DB_KEY_NOTIFICATION_UUID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NOTIFICATION_UUID)]
	public string NotificationUUID { get; set; }
}
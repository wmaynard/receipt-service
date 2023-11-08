using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

[BsonIgnoreExtraElements]
public class ChargebackLog : PlatformCollectionDocument
{
	internal const string DB_KEY_ACCOUNT_ID       = "aid";
	internal const string DB_KEY_ORDER_ID         = "oid";
	internal const string DB_KEY_VOIDED_TIMESTAMP = "voidTs";
	internal const string DB_KEY_REASON           = "rsn";
	internal const string DB_KEY_SOURCE           = "src";
	internal const string DB_KEY_TIMESTAMP        = "ts";
	internal const string DB_KEY_UNBANNED         = "unban";

	public const string FRIENDLY_KEY_ACCOUNT_ID       = "accountId";
	public const string FRIENDLY_KEY_ORDER_ID         = "orderId";
	public const string FRIENDLY_KEY_VOIDED_TIMESTAMP = "voidedTimestamp";
	public const string FRIENDLY_KEY_REASON           = "reason";
	public const string FRIENDLY_KEY_SOURCE           = "source";
	public const string FRIENDLY_KEY_TIMESTAMP        = "timestamp";
	public const string FRIENDLY_KEY_UNBANNED         = "unbanned";
	
	[SimpleIndex]
	[CompoundIndex(@group: "INDEX_GROUP_CHARGEBACK", priority: 0)]
	[BsonElement(DB_KEY_ACCOUNT_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACCOUNT_ID)]
	public string AccountId { get; set; }
	
	[BsonElement(DB_KEY_ORDER_ID)]
	[JsonInclude, JsonPropertyName((FRIENDLY_KEY_ORDER_ID))]
	public string OrderId { get; set; }
	
	[BsonElement(DB_KEY_VOIDED_TIMESTAMP)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_VOIDED_TIMESTAMP)]
	public long VoidedTimestamp { get; set; }
	
	[BsonElement(DB_KEY_REASON)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_REASON)]
	public string Reason { get; set; }
	
	[BsonElement(DB_KEY_SOURCE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SOURCE)]
	public string Source { get; set; }
	
	[CompoundIndex(@group: "INDEX_GROUP_CHARGEBACK", priority: 1)]
	[BsonElement(DB_KEY_TIMESTAMP)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
	public long Timestamp { get; set; }
	
	[BsonElement(DB_KEY_UNBANNED)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_UNBANNED)]
	public bool Unbanned { get; set; }
	
	// public ChargebackLog(string accountId, string orderId, long voidedTimestamp, string reason, string source, bool unbanned = false)
	// {
	// 	AccountId = accountId;
	// 	OrderId = orderId;
	// 	VoidedTimestamp = voidedTimestamp;
	// 	Reason = reason;
	// 	Source = source;
	// 	Timestamp = Common.Utilities.Timestamp.UnixTime;
	// 	Unbanned = unbanned;
	// }
}
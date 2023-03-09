using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models;

public class ForcedValidation : PlatformCollectionDocument
{
	internal const string DB_KEY_TRANSACTION_ID = "txId";

	public const string FRIENDLY_KEY_TRANSACTION_ID = "transactionId";
	
	[BsonElement(DB_KEY_TRANSACTION_ID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TRANSACTION_ID)]
	public string TransactionId { get; set; }

	public ForcedValidation(string transactionId)
	{
		TransactionId = transactionId;
	}
}
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

public class PaginationToken : PlatformDataModel
{
	internal const string DB_KEY_NEXT_PAGE_TOKEN = "nxtPgTkn";

	public const string FRIENDLY_KEY_NEXT_PAGE_TOKEN = "nextPageToken";
	
	[BsonElement(DB_KEY_NEXT_PAGE_TOKEN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_NEXT_PAGE_TOKEN)]
	public string NextPageToken { get; set; }
}
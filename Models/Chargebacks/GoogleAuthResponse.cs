using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

namespace Rumble.Platform.ReceiptService.Models.Chargebacks;

public class GoogleAuthResponse : PlatformDataModel
{
	internal const string DB_KEY_ACCESS_TOKEN = "acsTkn";
	internal const string DB_KEY_SCOPE = "scope";
	internal const string DB_KEY_TOKEN_TYPE = "tknType";
	internal const string DB_KEY_EXPIRES_IN = "expIn";
		
	public const string FRIENDLY_KEY_ACCESS_TOKEN = "access_token";
	public const string FRIENDLY_KEY_SCOPE = "scope";
	public const string FRIENDLY_KEY_TOKEN_TYPE = "token_type";
	public const string FRIENDLY_KEY_EXPIRES_IN = "expires_in";
	
	[BsonElement(DB_KEY_ACCESS_TOKEN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACCESS_TOKEN)]
	public string AccessToken { get; set; }
	
	[BsonElement(DB_KEY_SCOPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_SCOPE)]
	public string Scope { get; set; }
	
	[BsonElement(DB_KEY_TOKEN_TYPE)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TOKEN_TYPE)]
	public string TokenType { get; set; }
	
	[BsonElement(DB_KEY_EXPIRES_IN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRES_IN)]
	public long ExpiresIn { get; set; }
}
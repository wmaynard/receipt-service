using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Models.Chargebacks;
using Rumble.Platform.ReceiptService.Utilities;

namespace Rumble.Platform.ReceiptService.Services;

public class GoogleChargebackService : QueueService<GoogleChargebackService.ChargebackData>
{
#pragma warning disable
	private readonly ApiService           _apiService;
	private readonly DynamicConfig        _dynamicConfig;
	private readonly ChargebackLogService _chargebackLogService;
	private readonly ReceiptService       _receiptService;
	private SlackMessageClient   _slackMessageClient;
#pragma warning restore

	public const int  CONFIG_TIME_BUFFER = 30_000; // time in ms between requests
	public const int  CONFIG_MAX_RESULTS = 1_000; // defaults to 1000
	public const int  CONFIG_TYPE        = 0;     // default 0: only voided iap, 1: voided iap and subscriptions

	private string _nextPageToken = null; // used if over maximum results
	private long _tokenExpireTime = UnixTime; // in seconds, used for fetching a new auth token when previous expires
	private long _startTime = UnixTimeMS - 86_400_000; // in milliseconds, start time of requested voided purchases, set to start a day before to cover downtime. to be updated every pass
	private string _authToken = null;
	
	public GoogleChargebackService() : base(collection: "chargebacks", primaryNodeTaskCount: 10, secondaryNodeTaskCount: 0, intervalMs: CONFIG_TIME_BUFFER) { }
	
	protected override void OnTasksCompleted(ChargebackData[] data)
	{
		int processedCount = data.Length;
		
		Log.Info(owner: Owner.Nathan, message: "Google chargebacks processed.", data: $"Processed count: {processedCount}.");
	}

	protected override void PrimaryNodeWork()
	{
		// Use Google service account credentials to fetch auth token -- expires in the duration provided in `expires_in` field
		// Need to base64 url encode the credentials before sending

		if (UnixTime >= _tokenExpireTime || _authToken == null) // when auth token is expired, fetch new one
		{
			RumbleJson headerJson = new RumbleJson
			                        {
				                        { "typ", "JWT" }
			                        };

			RumbleJson claimsJson = new RumbleJson
			                        {
				                        { "iss", PlatformEnvironment.Require<string>(key: "googleServiceAccountClientEmail") },
				                        { "scope", "https://www.googleapis.com/auth/androidpublisher" },
				                        { "aud", "https://oauth2.googleapis.com/token" }, // always the same
				                        { "exp", UnixTime + 3_600 }, // in seconds, one hour expiration time -- maximum is one hour
				                        { "iat", UnixTime } // in seconds
			                        };

			string privateKey = PlatformEnvironment.Require<string>(key: "googleServiceAccountPrivateKey");

			string jwt;
			try
			{
				jwt = GenerateJWT.GenerateJWTToken(header: headerJson, payload: claimsJson, rsaPrivateKey: privateKey);
			}
			catch (Exception e)
			{
				Log.Error(owner: Owner.Nathan, message: "Error occurred attempting to generate JWT to fetch Google service account auth key.", data: $"Exception: {e}. Header: {headerJson}. Claims: {claimsJson}.");
				throw new PlatformException(message:
				                            "Error occurred attempting to generate JWT to fetch Google service account auth key.");
			}
			RumbleJson authPayload = new RumbleJson
			                            {
				                            {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"},
				                            {"assertion", jwt}
			                            };
			
			_apiService
				.Request(url: "https://oauth2.googleapis.com/token") // always the same
				.SetPayload(authPayload)
				.OnFailure(response =>
				{
					Log.Error(Owner.Nathan, message: "Unable to fetch Google service account auth token.", data: new
					{
						Response = response.AsRumbleJson
					});
				})
				.Post(out GoogleAuthResponse authRes, out int authCode);

			_authToken = authRes.AccessToken;
			_tokenExpireTime += authRes.ExpiresIn;
		}

		// The default maximum number of purchases that appear is 1000. There is a nextPageToken that can be passed into further requests to view more results
		// Results are shown from oldest to newest, with a default startTime of 30 days ago
		// Google limits to 6000 queries a day, and 30 queries in any 30 second period

		RumbleJson parameters = new RumbleJson
		                        {
									{ "startTime", _startTime },
									{ "maxResults", CONFIG_MAX_RESULTS },
									{ "type", CONFIG_TYPE }
								};
		
		_apiService
			// url with package name in dynamic config
			// https://www.googleapis.com/androidpublisher/v3/applications/your_package_name/purchases/voidedpurchases?access_token=your_auth_token
			.Request(url: _dynamicConfig.Require<string>(key:"googleVoidedPurchasesUrl") + _authToken)
			.AddAuthorization(_authToken)
			.AddParameters(parameters)
			.OnFailure(response =>
			{
				Log.Error(Owner.Nathan, message: "Unable to fetch Google voided purchases.", data: new
				{
					Response = response.AsRumbleJson
				});
			})
			.Get(out RumbleJson res, out int code);

		if (!code.Between(200, 299))
		{
			Log.Error(owner: Owner.Nathan, message: "Alert was supposed to be triggered for fetching voided purchases.", data: $"Code: {code}. Response: {res}");
			
			_apiService.Alert(
				title: "Unable to fetch Google voided purchases.",
				message: "Unable to fetch Google voided purchases. Google's API may be down.",
				countRequired: 10,
				timeframe: 300,
				data: new RumbleJson
				      {
					      { "code", code }
				      } ,
				owner: Owner.Nathan
			);
		}
		
		if (res.Optional<List<ChargebackData>>(key: "voidedPurchases") != null)
		{
			foreach (ChargebackData data in res.Optional<List<ChargebackData>>(key: "voidedPurchases"))
			{
				if (_chargebackLogService.Find(log => log.OrderId == data.OrderId).FirstOrDefault() == null && _receiptService.Find(receipt => receipt.OrderId == data.OrderId).FirstOrDefault() != null)
				{
					CreateTask(data);
				}
			}
		}
		
		if (res.Optional<PaginationToken>("tokenPagination") != null)
		{
			_nextPageToken = res.Optional<PaginationToken>(key: "tokenPagination").NextPageToken;
		}
		else
		{
			_nextPageToken = null;
		}
		
		while (_nextPageToken != null)
		{
			parameters["token"] = _nextPageToken;
			
			_apiService
				// url with package name in dynamic config
				// https://www.googleapis.com/androidpublisher/v3/applications/your_package_name/purchases/voidedpurchases?access_token=your_auth_token
				.Request(url: _dynamicConfig.Require<string>(key:"googleVoidedPurchasesUrl") + _authToken)
				.AddAuthorization(_authToken)
				.AddParameters(parameters)
				.OnFailure(response =>
	            {
		            Log.Error(Owner.Nathan, message: "Unable to fetch Google voided purchases.", data: new
	                {
						Response = response.AsRumbleJson
	                });
	            })
				.Get(out RumbleJson nextRes, out int nextCode);

			if (!code.Between(200, 299))
			{
				Log.Error(owner: Owner.Nathan, message: "Alert was supposed to be triggered for fetching voided purchases.", data: $"Code: {code}. Response: {nextRes}");
				
				_apiService.Alert(
				                  title: "Unable to fetch Google voided purchases.",
				                  message: "Unable to fetch Google voided purchases. Google's API may be down.",
				                  countRequired: 10,
				                  timeframe: 300,
				                  data: new RumbleJson
				                        {
					                        { "code", nextCode }
				                        } ,
				                  owner: Owner.Nathan
				                 );
			}

			if (nextRes.Optional<List<ChargebackData>>(key: "voidedPurchases") != null)
			{
				foreach (ChargebackData data in nextRes.Optional<List<ChargebackData>>(key: "voidedPurchases"))
				{
					if (_chargebackLogService.Find(log => log.OrderId == data.OrderId).FirstOrDefault() == null && _receiptService.Find(receipt => receipt.OrderId == data.OrderId).FirstOrDefault() != null)
					{
							CreateTask(data);
					}
				}
			}
			
			if (nextRes.Optional<PaginationToken>("tokenPagination") != null)
			{
				_nextPageToken = nextRes.Optional<PaginationToken>(key: "tokenPagination").NextPageToken;
			}
			else
			{
				_nextPageToken = null;
			}
		}

		_startTime = UnixTimeMS - 86_400_000; // sets new start time for next pass
	}	

	protected override void ProcessTask(ChargebackData data)
	{
		string orderId = data.OrderId;

		if (_chargebackLogService.Find(log => log.OrderId == orderId).FirstOrDefault() == null)
		{
			string accountId = _receiptService.GetAccountIdByOrderId(orderId);
			
			_apiService.BanPlayer(accountId);

			ChargebackLog chargebackLog = new ChargebackLog(
	            accountId: accountId,
	            orderId: orderId,
	            voidedTimestamp: data.VoidedTimeMillis,
	            reason: data.VoidedReason.ToString(),
	            source: data.VoidedSource.ToString()
			);
			_chargebackLogService.Create(chargebackLog);

			_slackMessageClient = new SlackMessageClient(
				channel:
				PlatformEnvironment.Require<string>(key: "slackChannel") ??
				PlatformEnvironment.SlackLogChannel,
				token: PlatformEnvironment.SlackLogBotToken
            );

			List<SlackBlock> slackHeaders = new List<SlackBlock>()
            {
                new(SlackBlock.BlockType.HEADER,
                    $"{PlatformEnvironment.Deployment} | Chargeback Banned Player | {DateTime.Now:yyyy.MM.dd HH:mm}"),
                new($"*Banned Player*: {accountId}\n*Source*: Google\n*Owners:* {string.Join(", ", _slackMessageClient.UserSearch(Owner.Nathan).Select(user => user.Tag))}"),
                new(SlackBlock.BlockType.DIVIDER)
            };
			List<SlackBlock> slackBlocks = new List<SlackBlock>();
			slackBlocks.Add(new SlackBlock(text:
				$"*AccountId*: {accountId}\n*OrderId*: {orderId}\n*Voided Timestamp*: {data.VoidedTimeMillis}\n*Reason*: {data.VoidedReason.ToString()}\n*Source*: {data.VoidedSource.ToString()}")
			);

			SlackMessage slackMessage = new SlackMessage(
				blocks: slackHeaders,
				attachments: new SlackAttachment("#2eb886", slackBlocks)
            );

			_slackMessageClient.Send(message: slackMessage);
		}
	}

	public class ChargebackData : PlatformDataModel // public because of accessibilty?
	{
		[BsonElement("kind")]
		[JsonInclude, JsonPropertyName("kind")]
		public string Kind { get; set; }
		
		[BsonElement("purchaseToken")]
		[JsonInclude, JsonPropertyName("purchaseToken")]
		public string PurchaseToken { get; set; }
		
		[BsonElement("purchaseTimeMillis")]
		[JsonInclude, JsonPropertyName("purchaseTimeMillis")]
		public long PurchaseTimeMillis { get; set; }
		
		[BsonElement("voidedTimeMillis")]
		[JsonInclude, JsonPropertyName("voidedTimeMillis")]
		public long VoidedTimeMillis { get; set; }
		
		[BsonElement("orderId")]
		[JsonInclude, JsonPropertyName("orderId")]
		public string OrderId { get; set; }
		
		public enum VoidedSources
		{
			User,
			Developer,
			Google
		}
		[BsonElement("voidedSource")]
		[JsonInclude, JsonPropertyName("voidedSource")]
		public VoidedSources VoidedSource { get; set; } // enum; 0: User, 1: Developer, 2: Google
		
		public enum VoidedReasons
		{
			Other,
			Remorse,
			NotReceived,
			Defective,
			AccidentalPurchase,
			Fraud,
			FriendlyFraud,
			Chargeback
		}
		[BsonElement("voidedReason")]
		[JsonInclude, JsonPropertyName("voidedReason")]
		public VoidedReasons VoidedReason { get; set; } // enum; 0: Other, 1: Remorse, 2: Not_received, 3: Defective, 4: Accidental_purchase, 5: Fraud, 6: Friendly_fraud, 7: Chargeback
	}
}
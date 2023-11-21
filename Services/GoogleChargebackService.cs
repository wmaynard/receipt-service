using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	private SlackMessageClient            _slackMessageClient;
#pragma warning restore
	
	public const int CONFIG_TIME_BUFFER          = 60_000; // time in ms between requests
	public const int CONFIG_TIME_BUFFER_NON_PROD = 600_000; // time in ms between requests for non prod environments
	public const int CONFIG_MAX_RESULTS          = 1_000; // defaults to 1000
	public const int CONFIG_TYPE                 = 0;     // default 0: only voided iap, 1: voided iap and subscriptions

	private string _nextPageToken = null; // used if over maximum results
	private long _tokenExpireTime = Timestamp.Now; // in seconds, used for fetching a new auth token when previous expires
	private long _startTime = TimestampMs.OneDayAgo; // in milliseconds, start time of requested voided purchases, set to start a day before to cover downtime. to be updated every pass
	private string _authToken = null;

	public GoogleChargebackService() : base(collection: "chargebacks", primaryNodeTaskCount: 10, secondaryNodeTaskCount: 0, intervalMs: PlatformEnvironment.IsProd ? CONFIG_TIME_BUFFER : CONFIG_TIME_BUFFER_NON_PROD)
	{
		_slackMessageClient = new SlackMessageClient(
			channel: PlatformEnvironment.Optional<string>(key: "slackChannel") ?? PlatformEnvironment.SlackLogChannel,
			token: PlatformEnvironment.SlackLogBotToken
		);
		#if DEBUG
		Pause();
		#endif
	}
	
	protected override void OnTasksCompleted(ChargebackData[] data) =>
		Log.Info(Owner.Will, "Google chargebacks processed.", data: new
		{
			ProcessedCount = data.Length
		});

	/// <summary>
	/// when auth token is expired, fetch new one
	/// </summary>
	/// <exception cref="PlatformException"></exception>
	private void RefreshAuthToken()
	{
		if (Timestamp.Now < _tokenExpireTime && _authToken != null)
			return;
		
		string jwt;
		try
		{
			jwt = GenerateJWT.GenerateJWTToken(
				header: new RumbleJson {{ "typ", "JWT" }}, 
				payload: new RumbleJson
				{
					{ "iss", PlatformEnvironment.Require<string>(key: "googleServiceAccountClientEmail") },
					{ "scope", "https://www.googleapis.com/auth/androidpublisher" },
					{ "aud", "https://oauth2.googleapis.com/token" }, // always the same
					{ "exp", Timestamp.OneHourFromNow }, // in seconds, one hour expiration time -- maximum is one hour
					{ "iat", Timestamp.Now }
				},
				rsaPrivateKey: PlatformEnvironment.Require<string>(key: "googleServiceAccountPrivateKey")
			);
		}
		catch (Exception e)
		{
			throw new PlatformException("Error occurred attempting to generate JWT to fetch Google service account auth key.", inner: e);
		}
			
		_apiService
			.Request(url: "https://oauth2.googleapis.com/token") // always the same
			.SetPayload(new RumbleJson
			{
				{"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"},
				{"assertion", jwt}
			})
			.OnFailure(response =>
			{
				Log.Error(Owner.Will, "Unable to fetch Google service account auth token.", data: new
				{
					Response = response
				});
			})
			.Post(out GoogleAuthResponse authRes, out _);

		_authToken = authRes.AccessToken;
		_tokenExpireTime += authRes.ExpiresIn;
	}
	protected override void PrimaryNodeWork()
	{
		// Use Google service account credentials to fetch auth token -- expires in the duration provided in `expires_in` field
		// Need to base64 url encode the credentials before sending

		RefreshAuthToken();
		
		// The default maximum number of purchases that appear is 1000. There is a nextPageToken that can be passed into further requests to view more results
		// Results are shown from oldest to newest, with a default startTime of 30 days ago
		// Google limits to 6000 queries a day, and 30 queries in any 30 second period

		RumbleJson parameters = new()
        {
			{ "startTime", _startTime },
			{ "maxResults", CONFIG_MAX_RESULTS },
			{ "type", CONFIG_TYPE }
		};

		string url = _dynamicConfig.Require<string>(key: "googleVoidedPurchasesUrl") + _authToken;
		do
		{
			_apiService
				// url with package name in dynamic config
				// https://www.googleapis.com/androidpublisher/v3/applications/your_package_name/purchases/voidedpurchases?access_token=your_auth_token
				.Request(url)
				.AddAuthorization(_authToken)
				.AddParameters(parameters)
				.OnFailure(response => _apiService.Alert(
					title: "Unable to fetch Google voided purchases.",
					message: "Unable to fetch Google voided purchases. Google's API may be down.",
					countRequired: 10,
					timeframe: 300,
					data: new RumbleJson
					{
						{ "response", response }
					},
					owner: Owner.Will
				))
				.Get(out RumbleJson nextRes, out int nextCode);

			List<ChargebackData> chargebacks = nextRes.Optional<List<ChargebackData>>("voidedPurchases") ?? new List<ChargebackData>();

			string[] newIds = chargebacks
				.Select(chargeback => chargeback.OrderId)
				.ToArray();
			newIds = _chargebackLogService.RemoveExistingIdsFrom(newIds);
			
			if (newIds.Any())
				Log.Info(Owner.Will, "Found new GPG chargebacks to process.", data: new
				{
					newIdCount = newIds.Length,
					chargebackLogCount = chargebacks.Count
				});

			chargebacks
				.Where(chargeback => newIds.Contains(chargeback.OrderId))
				.ToList()
				.ForEach(CreateTask);
			
			parameters["token"] = _nextPageToken = nextRes.Optional<PaginationToken>("tokenPagination")?.NextPageToken;
		} while (_nextPageToken != null);
		

		_startTime = TimestampMs.Now - 86_400_000; // sets new start time for next pass
	}

	private void SendNotification(string accountId, ChargebackData data)
	{
		string additionalOwners = _slackMessageClient?.UserSearch("massey").FirstOrDefault()?.Tag ?? "";
		
		if (!string.IsNullOrWhiteSpace(additionalOwners))
			additionalOwners = $"FYI {additionalOwners}";
		
		SlackDiagnostics
			.Log(
				title: $"{PlatformEnvironment.Deployment} | Chargeback Banned Player | {DateTime.Now:yyyy.MM.dd HH:mm}", 
				message: additionalOwners
			)
			.Tag(Owner.Will)
			.Attach("Details", $@"    Account ID: {accountId}
       OrderId: {data.OrderId}
     Singleton: {GetType().Name}
Timestamp (ms): {data.VoidedTimeMillis}
        Reason: {data.VoidedReason}
        Source: {data.VoidedSource}"
			)
			// .Send("C02C18NDJKY") // #slack-app-sandbox
			.Send(PlatformEnvironment.Require<string>(key: "slackChannel") ?? PlatformEnvironment.SlackLogChannel)
			.Wait();
	}

	protected override void ProcessTask(ChargebackData data)
	{
		_receiptService.GetAccountIdFor(data.OrderId, out string accountId);

		if (!string.IsNullOrWhiteSpace(accountId))
		{
			_apiService.BanPlayer(accountId, reason: "GPG chargeback");
			SendNotification(accountId, data);
			Log.Info(Owner.Will, "Chargeback processed, player banned", data: new
			{
				ChargebackData = data,
				AccountId = accountId
			});
		}
		else
			Log.Local(Owner.Will, $"Account ID was null, otherwise I'd notify someone of chargeback {data.OrderId}.", emphasis: Log.LogType.ERROR);
		
		_chargebackLogService.Insert(new ChargebackLog
		{
			AccountId = accountId ?? "other env",
			OrderId = data.OrderId,
			VoidedTimestamp = data.VoidedTimeMillis,
			Reason = data.VoidedReason.ToString(),
			Source = data.VoidedSource.ToString()
		});
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
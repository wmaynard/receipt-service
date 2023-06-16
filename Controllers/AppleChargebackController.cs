using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.ReceiptService.Models.Chargebacks;
using Rumble.Platform.ReceiptService.Services;

namespace Rumble.Platform.ReceiptService.Controllers;

[Route("commerce/chargeback/apple")]
public class AppleChargebackController : PlatformController
{
#pragma warning disable
	private readonly ApiService              _apiService;
	private readonly ChargebackLogService    _chargebackLogService;
	private readonly Services.ReceiptService _receiptService;
	private SlackMessageClient      _slackMessageClient;
#pragma warning restore
	
	// Listens for chargeback notifications from Apple
	// Allow IP address subnet 17.0.0.0/8
	[HttpPost, Route(template: ""), NoAuth]
	public ObjectResult ChargebackApple()
	{
		// base64 url decode to get decodedpayload - contains notificationType, subtype, metadata, data/summary
		// data contains signedTransactionInfo(JWSTransaction) to be base64 url decode for transaction details
		// signed JWS representations have a signature to validate with header's alg parameter
		string signedPayload = Require<string>(key: "signedPayload");

		try
		{
			byte[] bufferPayload = Convert.FromBase64String(signedPayload);
			string decodedPayload = Encoding.UTF8.GetString(bufferPayload);
			AppleChargeback appleChargeback = ((RumbleJson) decodedPayload).ToModel<AppleChargeback>();

			byte[] bufferRenewalInfo = Convert.FromBase64String(appleChargeback.Data.JWSRenewalInfo);
			string decodedRenewalInfo = Encoding.UTF8.GetString(bufferRenewalInfo);
			AppleRenewalInfo appleRenewalInfo = ((RumbleJson) decodedRenewalInfo).ToModel<AppleRenewalInfo>(); // for subscriptions, not yet used
			
			byte[] bufferTransactionInfo = Convert.FromBase64String(appleChargeback.Data.JWSTransaction);
			string decodedTransactionInfo = Encoding.UTF8.GetString(bufferTransactionInfo);
			AppleTransactionInfo appleTransactionInfo = ((RumbleJson) decodedTransactionInfo).ToModel<AppleTransactionInfo>();

			string transactionId = appleTransactionInfo.OriginalTransactionId;
			
			string accountId = _receiptService.GetAccountIdByOrderId(orderId: transactionId);
			
			_apiService.BanPlayer(accountId);
			ChargebackLog chargebackLog = new ChargebackLog(
				accountId: accountId,
				orderId: transactionId,
				voidedTimestamp: appleTransactionInfo.RevocationDate,
				reason: appleTransactionInfo.RevocationReason.ToString(),
				source: "Apple"
			);
			_chargebackLogService.Create(chargebackLog);

			_slackMessageClient = new SlackMessageClient(
		         channel: PlatformEnvironment.Require<string>(key: "slackChannel") ?? PlatformEnvironment.SlackLogChannel,
		         token: PlatformEnvironment.SlackLogBotToken
	        );
			
			List<SlackBlock> slackHeaders = new List<SlackBlock>()
	        {
	            new(SlackBlock.BlockType.HEADER, $"{PlatformEnvironment.Deployment} | Chargeback Banned Player | {DateTime.Now:yyyy.MM.dd HH:mm}"),
	            new($"*Banned Player*: {accountId}\n*Source*: Apple\n*Owners:* {string.Join(", ", _slackMessageClient.UserSearch(Owner.Nathan).Select(user => user.Tag))}"),
	            new(SlackBlock.BlockType.DIVIDER)
	        };
			List<SlackBlock> slackBlocks = new List<SlackBlock>();
			slackBlocks.Add(new SlackBlock(text: $"*AccountId*: {accountId}\n*TransactionId*: {transactionId}\n*Voided Timestamp*: {appleTransactionInfo.RevocationDate}\n*Reason*: {appleTransactionInfo.RevocationReason.ToString()}\n*Source*: Apple"));

			SlackMessage slackMessage = new SlackMessage(
				blocks: slackHeaders,
				attachments: new SlackAttachment("#2eb886", slackBlocks)
			);

			_slackMessageClient.Send(message: slackMessage);
			
		}
		catch (Exception e)
		{
			Log.Error(owner: Owner.Nathan, message: "Error occurred when attempting to process Apple chargeback.", data: $"Exception: {e}");
			_apiService.Alert(
				title: "Error occurred when attempting to process Apple chargeback.",
				message: "Error occurred when attempting to process Apple chargeback. Either there is an issue processing Apple's signed payload or there is a malicious actor",
				countRequired: 10,
				timeframe: 300,
				data: new RumbleJson
				    {
				        { "exception", e }
				    } ,
				owner: Owner.Nathan
			);
		}
		return Ok();
	}
}
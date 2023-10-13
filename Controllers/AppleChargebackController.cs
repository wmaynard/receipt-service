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
using Rumble.Platform.ReceiptService.Utilities;

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
		// TODO look into different notification type handling, especially when we start dealing with subscriptions--there may be more complications to handle
		// TODO https://developer.apple.com/documentation/appstoreservernotifications/notificationtype
		
		// base64 url decode to get decodedpayload - contains notificationType, subtype, metadata, data/summary
		// data contains signedTransactionInfo(JWSTransaction) to be base64 url decode for transaction details
		// signed JWS representations have a signature to validate with header's alg parameter
		string signedPayload = Require<string>(key: "signedPayload");
		
		Log.Warn(Owner.Will, "Chargeback notification received from Apple.", data: new
		{
			Payload = signedPayload
		});
		
		try
		{
			string[] split = signedPayload.Split(separator: ".");

			string header = split[0];
			string payload = split[1];
			string signature = split[2];
			
			byte[] bufferPayload = DecodeUrlBase64.Decode(payload);
			string decodedPayload = Encoding.UTF8.GetString(bufferPayload);
			Log.Info(Owner.Will, "Apple chargeback payload decoded.", data: new
			{
				Payload = decodedPayload
			}); // TODO remove when no longer needed
			AppleChargeback appleChargeback = ((RumbleJson) decodedPayload).ToModel<AppleChargeback>();

			if (appleChargeback.Data.JWSRenewalInfo != null)
			{
				byte[] bufferRenewalInfo = DecodeUrlBase64.Decode(appleChargeback.Data.JWSRenewalInfo);
				string decodedRenewalInfo = Encoding.UTF8.GetString(bufferRenewalInfo);
				AppleRenewalInfo appleRenewalInfo = ((RumbleJson) decodedRenewalInfo).ToModel<AppleRenewalInfo>();
				
				// TODO implement handling when/if we add in subscriptions
				_apiService.Alert(
					title: "A chargeback was detected for a subscription.",
					message: "A chargeback was detected for a subscription. This is not expected and thus automatic handling is not yet implemented.",
					countRequired: 1,
					timeframe: 300,
					data: new RumbleJson
					{
						{ "Apple Notification", appleChargeback },
						{ "Renewal Info", appleRenewalInfo }
					} ,
					owner: Owner.Will
				);
			}

			if (appleChargeback.NotificationType == "REFUND" && appleChargeback.Data.JWSTransaction != null)
			{
				string[] transactionSplit = appleChargeback.Data.JWSTransaction.Split(separator: ".");

				string transactionHeader = transactionSplit[0];
				string transactionPayload = transactionSplit[1];
				string transactionSignature = transactionSplit[2];

				byte[] bufferTransactionInfo = DecodeUrlBase64.Decode(transactionPayload);
				string decodedTransactionInfo = Encoding.UTF8.GetString(bufferTransactionInfo);
				
				AppleTransactionInfo appleTransactionInfo = ((RumbleJson) decodedTransactionInfo).ToModel<AppleTransactionInfo>();

				string transactionId = appleTransactionInfo.OriginalTransactionId;

				string accountId = _receiptService.GetAccountIdByOrderId(orderId: transactionId);

				_apiService.BanPlayer(accountId, reason: "iOS chargeback");

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
                    new($"*Banned Player*: {accountId}\n*Source*: Apple\n*Owners:* {string.Join(", ", _slackMessageClient.UserSearch(Owner.Will).Select(user => user.Tag))}"),
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
			
			return Ok();
		}
		catch (Exception e)
		{
			_apiService.Alert(
				title: "Error occurred when attempting to process Apple chargeback.",
				message: "Error occurred when attempting to process Apple chargeback. Either there is an issue processing Apple's signed payload or there is a malicious actor.",
				countRequired: 1,
				timeframe: 300,
				data: new RumbleJson
			    {
			        { "exception", e }
			    } ,
				owner: Owner.Will
			);

			return Problem();
		}
	}
}
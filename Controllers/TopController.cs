using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Services;

namespace Rumble.Platform.ReceiptService.Controllers
{
    [ApiController, Route(template: "commerce/receipt"), RequireAuth, UseMongoTransaction]
    public class TopController : PlatformController
    {
        private readonly AppleService _appleService;
        private readonly GoogleService _googleService;
        private readonly SamsungService _samsungService;
        private readonly RedisService _redisService; // to be removed when no longer needed

        public TopController(
            AppleService appleService,
            GoogleService googleService,
            SamsungService samsungService,
            RedisService redisService, // to be removed when no longer needed
            IConfiguration config) : base(config)
        {
            _appleService = appleService;
            _googleService = googleService;
            _samsungService = samsungService;
            _redisService = redisService; // to be removed when no longer needed
        }

        [HttpGet, Route(template: "health"), NoAuth]
        public override ActionResult HealthCheck()
        {
            return Ok(
                _appleService.HealthCheckResponseObject,
                _googleService.HealthCheckResponseObject,
                _samsungService.HealthCheckResponseObject,
                _redisService.HealthCheckResponseObject // to be removed when no longer needed
            );
        }

        [HttpGet, Route(template: "redis"), RequireAuth((TokenType.ADMIN))] // to be removed when no longer needed
        public ActionResult UpdateFromRedis()
        {
            int counter;
            try
            {
                counter = _redisService.UpdateDatabase();
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: "Error occurred while attempting to update from Redis.", data: $"{e.Message}.");
                return Problem(detail: "Error occurred while attempting to update from Redis.");
            }
            return Ok(message: $"Data successfully fetched from Redis; {counter} new entries entered into Mongo.");
        }

        [HttpPost, Route(template: ""), RequireAuth(TokenType.ADMIN)]
        public async Task<ObjectResult> ReceiptVerify()
        {
            // the following are the current payload keys
            // if we need receipt to contain everything, perhaps key:receipt is not a receipt yet, but create receipt using these
            // also optional string signature for android

            string game = null;
            try
            {
                game = Require<string>(key: "game");
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message: "Error fetching game.", data: $"{e.Message}");
            }
            string accountId = Require<string>(key: "account"); // gukey
            string channel = Require<string>(key: "channel");
            string receiptData = Require<string>(key: "receipt"); // is stringified in the request
            Receipt receipt = JsonConvert.DeserializeObject<Receipt>(receiptData);

            // Receipt receipt = Require<Receipt>(key: "receipt");
            
            VerificationResult validated = null;
            
            Log.Info(owner: Owner.Nathan, message: $"Receipt validation request", data: $"game: {game}, accountId: {accountId}, channel: {channel}, receiptData: {receiptData}");
            Log.Info(owner: Owner.Nathan, message: $"Receipt parsed from receipt data", data: $"Receipt: {receipt}");

            if (game != "57901c6df82a45708018ba73b8d16004") // this is only for dev, different for each environment. fetch from dynamic config
            {
                return Problem(detail: $"Invalid game {game}.");
            }
            
            if (channel == "ios")
            {
                validated = await _appleService.VerifyApple(receipt: receipt);
                
                // response from apple
                // string environment (Production, Sandbox)
                // boolean is-retryable (0, 1) for status codes 21100-21199, 1 means try again, 0 means do not
                // byte latest_receipt (base64 encoded receipt) only for auto-renewable subscriptions
                // list latest_receipt_info (purchase transactions) only for auto-renewable subscriptions, does not include finished products
                // list pending_renewal_info (pending renewal information) only for auto-renewable subscriptions
                // json receipt (json) of receipt sent for verification
                // int status (0, status code) 0 if valid, status code if error; see https://developer.apple.com/documentation/appstorereceipts/status for status codes
                
                if (validated == null)
                {
                    Log.Error(owner: Owner.Nathan, message: "Error validating Apple receipt.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Error validating Apple receipt.");
                }

                if (validated.Status == "failed")
                {
                    Log.Error(owner: Owner.Nathan, message: "Failed to validate Apple receipt. Order does not exist.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Failed to validate Apple receipt.");
                }
                if (validated.Status == "success")
                {
                    Log.Info(owner: Owner.Nathan, message: "Successful Apple receipt processed.");
                    
                    if (_appleService.Exists(receipt?.OrderId))
                    {
                        Log.Error(owner: Owner.Nathan, message: "Apple receipt has already been redeemed.", data: $"Receipt: {receipt?.JSON}");
                        return Problem(detail: "Receipt has already been redeemed.");
                    }
                    
                    try
                    {
                        _appleService.Create(receipt);
                        return Ok(receipt?.ResponseObject);
                    }
                    catch (Exception e)
                    {
                        Log.Error(owner: Owner.Nathan, message: "Failed to record Apple receipt information.", data: $"{e.Message}. Receipt: {receipt?.JSON}");
                    }
                }
                
            }
            if (channel == "aos") // additionally looks at signature
            {
                string signature = Require<string>(key: "signature");
                validated = _googleService.VerifyGoogle(receipt: receipt, signature: signature);

                if (validated == null)
                {
                    Log.Error(owner: Owner.Nathan, message: "Error validating Google receipt.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Error validating Google receipt.");
                }
                if (validated.Status == "failed")
                {
                    Log.Error(owner: Owner.Nathan, message: "Failed to validate Google receipt. Order does not exist.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Failed to validate Google receipt.");
                }
                if (validated.Status == "success")
                {
                    Log.Info(owner: Owner.Nathan, message: "Successful Google receipt processed.");
                    
                    if (_googleService.Find(filter: receipt => receipt.OrderId == validated.TransactionId).FirstOrDefault() != null)
                    {
                        Log.Error(owner: Owner.Nathan, message: "Google receipt has already been redeemed.", data: $"Receipt: {receipt?.JSON}");
                        return Problem(detail: "Receipt has already been redeemed.");
                    }
                    
                    try
                    {
                        _googleService.Create(receipt);
                        return Ok(receipt?.ResponseObject);
                    }
                    catch (Exception e)
                    {
                        Log.Error(owner: Owner.Nathan, message: "Failed to record Google receipt information.", data: $"{e.Message}. Receipt: {receipt?.JSON}");
                        return Problem(detail: "Failed to record Google receipt information.");
                    }
                }
            }
            
            if (channel == "samsung") // old version additionally looks at playergukey(accountid?) for iosTestGroup_NoSaleOverride(?_
            {
                validated = await _samsungService.VerifySamsung(receipt: receipt);
                
                if (validated == null)
                {
                    Log.Error(owner: Owner.Nathan, message: "Error validating Samsung receipt.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Error validating Samsung receipt.");
                }

                if (validated.Status == "failed")
                {
                    Log.Error(owner: Owner.Nathan, message: "Failed to validate Samsung receipt. Order does not exist.", data: $"Receipt: {receipt?.JSON}");
                    return Problem(detail: "Failed to validate Samsung receipt.");
                }
                if (validated.Status == "success")
                {
                    Log.Info(owner: Owner.Nathan, message: "Successful Samsung receipt processed.");
                    
                    if (_samsungService.Exists(receipt?.OrderId))
                    {
                        Log.Error(owner: Owner.Nathan, message: "Samsung receipt has already been redeemed.", data: $"Receipt: {receipt?.JSON}");
                        return Problem(detail: "Receipt has already been redeemed.");
                    }
                    
                    try
                    {
                        _samsungService.Create(receipt);
                        return Ok(receipt.ResponseObject);
                    }
                    catch (Exception e)
                    {
                        Log.Error(owner: Owner.Nathan, message: "Failed to record Samsung receipt information.", data: $"{e.Message}. Receipt: {receipt?.JSON}");
                    }
                }
            }
            
            Log.Error(owner: Owner.Nathan, message: "Receipt called with invalid channel. Please use \"ios\", \"aos\", or \"samsung\" as the channel.");
            return Problem(detail: $"Invalid channel {channel}.");
        }
    }
}
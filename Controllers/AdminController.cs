using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Services;
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.ReceiptService.Controllers;

[Route("commerce/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
#pragma warning disable
    private readonly Services.ReceiptService _receiptService; // linter says Services. is needed?
    private readonly RedisService            _redisService;   // to be removed when no longer needed
    private readonly ForcedValidationService _forcedValidationService;
#pragma warning restore
    
    // Retrieves all entries in redis and moves them over to Mongo
    [HttpGet, Route(template: "redis"), IgnorePerformance] // to be removed when no longer needed
    public ActionResult UpdateFromRedis()
    {
        int counter;
        try
        {
            counter = _redisService.UpdateDatabase();
        }
        catch (Exception e)
        {
            throw new PlatformException("Error occurred while attempting to update from Redis.", inner: e);
        }
        return Ok(message: $"Data successfully fetched from Redis; {counter} new entries entered into Mongo.");
    }

    // Fetches all receipts in Mongo
    [HttpGet, Route(template: "all")]
    public ActionResult All()
    {
        List<Receipt> receipts = _receiptService.GetAll();
        
        return Ok(new { Receipts = receipts });
    }

    // Fetches all receipts in Mongo matching provided accountId
    [HttpGet, Route(template: "player")]
    public ActionResult Player()
    {
        string accountId = Require<string>(key: "accountId");
        
        List<Receipt> receipts = _receiptService.GetByAccount(accountId);

        return Ok(new { Receipts = receipts});
    }
    
    // Adds a transactionId to force validation
    [HttpPost, Route(template: "forceValidate")]
    public ActionResult ForceValidate()
    {
        string transactionId = Require<string>(key: "transactionId");

        ForcedValidation forcedValidation = new ForcedValidation(transactionId);

        _forcedValidationService.Create(forcedValidation);

        return Ok(message: "New transactionId added to forced transaction watchlist.");
    }
}
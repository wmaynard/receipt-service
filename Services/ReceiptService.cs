using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Models.Chargebacks;

namespace Rumble.Platform.ReceiptService.Services;

public class ReceiptService : MinqService<Receipt>
{
    public ReceiptService() : base("receipts") {  }

    public bool Exists(string orderId) => mongo.Count(query => query.EqualTo(receipt => receipt.OrderId, orderId)) > 0;

    public string GetAccountIdFor(string orderId, out string accountId) => accountId = mongo
		.Where(query => query.EqualTo(receipt => receipt.OrderId, orderId))
		.Limit(1)
		.Project(receipt => receipt.AccountId)
		.FirstOrDefault();
    
    public string GetAccountIdFor(Receipt receipt, out string accountId) => accountId = mongo
	    .Where(query => query.EqualTo(db => db.OrderId, receipt.OrderId))
	    .UpdateAndReturnOne(update => update
		    .Increment(dbReceipt => dbReceipt.ValidationCount, 1)
	    )
	    .AccountId;
    
    public string[] RemoveExistingIdsFrom(params string[] orderIds)
    {
	    if (orderIds == null || !orderIds.Any())
		    return Array.Empty<string>();

	    string[] existing = mongo
		    .Where(query => query.ContainedIn(receipt => receipt.OrderId, orderIds))
		    .Project(receipt => receipt.OrderId)
		    .ToArray();
	    
	    return orderIds.Except(existing).ToArray();
    }
    
	// Fetches receipts that match an accountId
	public List<Receipt> GetByAccount(string accountId) => mongo
		.Where(query => query.EqualTo(receipt => receipt.AccountId, accountId))
		.Sort(sort => sort.OrderByDescending(receipt => receipt.PurchaseTime))
		.Limit(500)
		.ToList();
}
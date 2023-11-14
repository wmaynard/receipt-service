using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;
using Rumble.Platform.ReceiptService.Models.Chargebacks;

namespace Rumble.Platform.ReceiptService.Services;

public class ReceiptService : PlatformMongoService<Receipt>
{
    public ReceiptService() : base("receipts") {  }

    public bool Exists(string orderId) => _collection.CountDocuments(receipt => receipt.OrderId == orderId) > 0;

    public string GetAccountIdFor(string orderId, out string accountId) => accountId = _collection
	    .Find(receipt => receipt.OrderId == orderId)
	    .Project(Builders<Receipt>.Projection.Expression(receipt => receipt.AccountId))
	    .FirstOrDefault();
    
    public string[] RemoveExistingIdsFrom(params string[] orderIds)
    {
	    if (orderIds == null || !orderIds.Any())
		    return Array.Empty<string>();
		
	    string[] existing = _collection
		    .Find(Builders<Receipt>.Filter.In(log => log.OrderId, orderIds))
		    .Project(Builders<Receipt>.Projection.Expression(log => log.OrderId))
		    .ToList()
		    .ToArray();
	    return orderIds.Except(existing).ToArray();
    }
  
	// Fetches receipts that match an accountId
	public List<Receipt> GetByAccount(string accountId)
    {
        return new List<Receipt>(_collection.Find(filter: receipt => receipt.AccountId == accountId)
                                            .ToList()
                                            .OrderByDescending(receipt => receipt.PurchaseTime));
    }

	// Fetches all receipts
	public List<Receipt> GetAll()
    {
        return new List<Receipt>(_collection.Find(filter: receipt => true)
                                            .ToList()
                                            .OrderByDescending(receipt => receipt.PurchaseTime));
    }
  
	// Fetches accountId that match an orderId
	public string GetAccountIdByOrderId(string orderId) => _collection
		.Find(Builders<Receipt>.Filter.Eq(receipt => receipt.OrderId, orderId))
		.FirstOrDefault()
		?.AccountId;
}
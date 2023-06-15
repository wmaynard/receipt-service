using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class ReceiptService : PlatformMongoService<Receipt>
{
    public ReceiptService() : base(collection: "receipts") {  }
  
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
	public string GetAccountIdByOrderId(string orderId)
	{
		return _collection.Find(filter: receipt => receipt.OrderId == orderId).FirstOrDefault().AccountId;
	}
}
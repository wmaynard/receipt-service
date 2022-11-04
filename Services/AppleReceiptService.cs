using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class AppleReceiptService : PlatformMongoService<AppleReceipt>
{
	public AppleReceiptService() : base(collection: "receipts") {  }
  
	// Fetches receipts that match an accountId
	public List<AppleReceipt> GetByAccount(string accountId)
	{
		return new List<AppleReceipt>(_collection.Find(filter: receipt => receipt.AccountId == accountId)
		                                    .ToList()
		                                    .OrderByDescending(receipt => receipt.ReceiptCreationDateMs));
	}

	// Fetches all receipts
	public List<AppleReceipt> GetAll()
	{
		return new List<AppleReceipt>(_collection.Find(filter: receipt => true)
		                                    .ToList()
		                                    .OrderByDescending(receipt => receipt.ReceiptCreationDateMs));
	}
}
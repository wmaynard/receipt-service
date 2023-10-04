using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Models.Chargebacks;

namespace Rumble.Platform.ReceiptService.Services;

public class ChargebackLogService : PlatformMongoService<ChargebackLog>
{
	public ChargebackLogService() : base(collection: "chargebackLogs") {  }

	public string[] RemoveExistingIdsFrom(params string[] orderIds)
	{
		if (orderIds == null || !orderIds.Any())
			return Array.Empty<string>();
		
		string[] existing = _collection
			.Find(Builders<ChargebackLog>.Filter.In(log => log.OrderId, orderIds))
			.Project(Builders<ChargebackLog>.Projection.Expression(log => log.OrderId))
			.ToList()
			.ToArray();
		return orderIds.Except(existing).ToArray();
	}
	
	// Fetch logs
	public List<ChargebackLog> GetLogs(bool unbanned)
	{
		if (unbanned)
		{
			return _collection.Find(log => true).ToList()
			                  .Select(log => log)
			                  .OrderBy(log => log.Timestamp)
			                  .ToList();
		}
		return _collection.Find(log => log.Unbanned == false).ToList()
		                  .Select(log => log)
		                  .OrderBy(log => log.Timestamp)
		                  .ToList();
	}
	
	// Fetch logs for an accountId
	public List<ChargebackLog> GetLogsByAccount(string accountId, bool unbanned)
	{
		if (unbanned)
		{
			return _collection.Find(log => log.AccountId == accountId).ToList()
							  .Select(log => log)
							  .OrderBy(log => log.Timestamp)
							  .ToList();
		}
		return _collection.Find(log => log.AccountId == accountId && log.Unbanned == false).ToList()
		                  .Select(log => log)
		                  .OrderBy(log => log.Timestamp)
		                  .ToList();

	}
	
	// Updates the unban field upon chargeback banned account being unbanned
	public void UnbanByAccount(string accountId)
	{
		_collection.UpdateMany(
           filter: Builders<ChargebackLog>.Filter.Eq(log => log.AccountId, accountId),
           update: Builders<ChargebackLog>.Update.Set(log => log.Unbanned, true)
        );
	}
}
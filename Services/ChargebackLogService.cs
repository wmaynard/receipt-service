using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models.Chargebacks;

namespace Rumble.Platform.ReceiptService.Services;

public class ChargebackLogService : PlatformMongoService<ChargebackLog>
{
	public ChargebackLogService() : base(collection: "chargebackLogs") {  }
	
	// Fetch logs
	public List<ChargebackLog> GetLogs()
	{
		return _collection.Find(log => true).ToList()
		                  .Select(log => log)
		                  .OrderBy(log => log.Timestamp)
		                  .ToList();
	}
	
	// Fetch logs for an accountId
	public List<ChargebackLog> GetLogsByAccount(string accountId)
	{
		return _collection.Find(log => log.AccountId == accountId).ToList()
		                  .Select(log => log)
		                  .OrderBy(log => log.Timestamp)
		                  .ToList();

	}
}
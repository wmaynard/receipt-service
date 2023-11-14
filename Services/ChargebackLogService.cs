using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.ReceiptService.Models.Chargebacks;

namespace Rumble.Platform.ReceiptService.Services;

public class ChargebackLogService : MinqService<ChargebackLog>
{
	public ChargebackLogService() : base("chargebackLogs") { }

	public string[] RemoveExistingIdsFrom(params string[] orderIds)
	{
		if (orderIds == null || !orderIds.Any())
			return Array.Empty<string>();

		return orderIds
			.Except(mongo
				.Where(query => query.ContainedIn(log => log.OrderId, orderIds))
				.Project(log => log.OrderId)
				.ToArray()
			)
			.ToArray();
	}

	public ChargebackLog[] ForAccount(string accountId) => mongo
		.Where(query => query.EqualTo(log => log.AccountId, accountId))
		.Sort(sort => sort.OrderByDescending(log => log.Timestamp))
		.Limit(1_000)
		.ToArray();
}
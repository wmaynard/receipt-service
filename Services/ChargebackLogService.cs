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

	public List<ChargebackLog> GetLogs(bool unbanned) => unbanned
		? mongo
			.All()
			.Sort(sort => sort.OrderBy(log => log.Timestamp))
			.ToList()
		: mongo
			.Where(query => query.EqualTo(log => log.Unbanned, false))
			.Sort(sort => sort.OrderBy(log => log.Timestamp))
			.ToList();

	public List<ChargebackLog> GetLogsByAccount(string accountId, bool unbanned) => mongo
		.Where(query =>
		{
			query.EqualTo(log => log.AccountId, accountId);
			if (unbanned)
				query.EqualTo(log => log.Unbanned, false);
		})
		.Sort(sort => sort.OrderBy(log => log.Timestamp))
		.ToList();

	public void UnbanByAccount(string accountId) => mongo
		.Where(query => query.EqualTo(log => log.AccountId, accountId))
		.Update(query => query.Set(log => log.Unbanned, true));

}
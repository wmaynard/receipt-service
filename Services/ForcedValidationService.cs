using MongoDB.Driver;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class ForcedValidationService : MinqService<ForcedValidation>
{
	// TODO: Include Admin tokens
	public ForcedValidationService() : base("forcedValidations") {  }

	
	public bool HasBeenForced(string transactionId) => mongo
		.Count(query => query.EqualTo(validation => validation.TransactionId, transactionId)) > 0;
}
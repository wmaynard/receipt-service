using MongoDB.Driver;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class ForcedValidationService : MinqService<ForcedValidation>
{
	// TODO: Include Admin tokens as a log item to indicate who forced the validation
	private readonly ReceiptService _receipt;

	public ForcedValidationService(ReceiptService receipt) : base("forcedValidations")
		=> _receipt = receipt;

	
	public bool HasBeenForced(string transactionId) => mongo
		.Count(query => query.EqualTo(validation => validation.TransactionId, transactionId)) > 0;

	public SuccessStatus CheckForForcedValidation(string orderId)
	{
		bool forced = mongo.Count(query => query.EqualTo(validation => validation.TransactionId, orderId)) > 0;

		if (!forced)
			return default;

		return _receipt.Exists(orderId)
			? SuccessStatus.Duplicated
			: SuccessStatus.True;
	}
}
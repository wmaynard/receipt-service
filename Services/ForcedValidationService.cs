using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Services;

public class ForcedValidationService : PlatformMongoService<ForcedValidation>
{
	public ForcedValidationService() : base(collection: "forcedValidations") {  }
	
	// Check if exists by transactionId
	public bool CheckTransactionId(string transactionId)
	{
		ForcedValidation forcedValidation = _collection.Find(filter: forcedValidation => forcedValidation.TransactionId == transactionId).FirstOrDefault();

		if (forcedValidation == null)
		{
			return false;
		}

		return true;
	}
}
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Exceptions;

public class ReceiptException : PlatformException
{
    public Receipt Receipt { get; init; }
    
    public ReceiptException(Receipt receipt, string message) : base(message, code: ErrorCode.ModelFailedValidation)
    {
        Receipt = receipt;
    }
}
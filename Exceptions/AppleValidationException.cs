using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.ReceiptService.Models;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Rumble.Platform.ReceiptService.Exceptions;

public class AppleReceiptException : PlatformException
{
	public AppleReceipt Receipt { get; init; }
    
	public AppleReceiptException(AppleReceipt receipt, string message) : base(message, code: ErrorCode.ModelFailedValidation)
	{
		Receipt = receipt;
	}
}
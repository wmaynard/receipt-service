using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.ReceiptService.Models;

namespace Rumble.Platform.ReceiptService.Exceptions;

public class BundleMismatchException : PlatformException
{
    public Receipt Receipt { get; set; }
    public string ExpectedBundle { get; set; }
    public string ReceivedBundle { get; set; }

    public BundleMismatchException(Receipt receipt, string message, string expected = null) : base(message, code: ErrorCode.InvalidRequestData)
    {
        Receipt = receipt;
        ExpectedBundle = expected;
        ReceivedBundle = receipt.PackageName;
    }
}
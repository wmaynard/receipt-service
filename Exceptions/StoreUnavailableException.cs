using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;

namespace Rumble.Platform.ReceiptService.Exceptions;

public class StoreUnavailableException : PlatformException
{
    public int HttpCode { get; set; }

    public StoreUnavailableException(int httpCode) : base("An external store is unavailable; the receipt cannot be processed.", code: ErrorCode.ApiFailure)
        => HttpCode = httpCode;
}
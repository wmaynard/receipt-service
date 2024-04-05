using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService;
public class Startup : PlatformStartup
{
    protected override PlatformOptions ConfigureOptions(PlatformOptions options) => options
        .SetProjectOwner(Owner.Will)
        .SetRegistrationName("Receipt V2")
        .SetTokenAudience(Audience.ReceiptService)
        .SetPerformanceThresholds(warnMS: 5_000, errorMS: 20_000, criticalMS: 300_000)
        .DisableServices(CommonService.Config)
        .DisableFeatures(CommonFeature.ConsoleObjectPrinting | CommonFeature.LogglyThrottling)
        .OnReady(_ =>
        {
        });
}
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService;
public class Startup : PlatformStartup
{
  protected override PlatformOptions Configure(PlatformOptions options) => options
   .SetProjectOwner(Owner.Nathan)
   .SetRegistrationName("ReceiptV2")
   .SetPerformanceThresholds(warnMS: 5_000, errorMS: 20_000, criticalMS: 300_000)
   .DisableServices(CommonService.Config);
}
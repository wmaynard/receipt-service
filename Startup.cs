using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService;
public class Startup : PlatformStartup
{
  protected override PlatformOptions Configure(PlatformOptions options) => options
   .SetProjectOwner(Owner.Nathan)
   .SetRegistrationName("ReceiptV2")
   .SetPerformanceThresholds(warnMS: 500, errorMS: 2_000, criticalMS: 30_000)
   .DisableServices(CommonService.Config);
}
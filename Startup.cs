using Microsoft.Extensions.DependencyInjection;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService
{
    public class Startup : PlatformStartup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
#if DEBUG
            base.ConfigureServices(services, Owner.Nathan, warnMS: 5_000, errorMS: 20_000, criticalMS: 300_000);
#else
                base.ConfigureServices(services, Owner.Nathan, warnMS: 500, errorMS: 2_000, criticalMS: 30_000);
#endif
        }
    }
}
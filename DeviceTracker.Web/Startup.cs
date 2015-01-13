using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DeviceTracker.Web.Startup))]
namespace DeviceTracker.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

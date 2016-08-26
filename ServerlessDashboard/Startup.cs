using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ServerlessDashboard.Startup))]
namespace ServerlessDashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}

using System.Configuration;
using System.Web.Mvc;
using System.Web.Routing;

namespace ServerlessDashboard
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Root",
                url: "",
                defaults: new {controller = "Home", action = "Index", id = ""}
            );

            routes.MapRoute(
                name: "ResultPoll",
                url: "getNewData/{testId}/{startDate}",
                defaults: new { controller = "Results", action = "GetNewResults" }
            );

            routes.MapRoute(
                name: "GetRunningTests",
                url: "getRunningTests/{timespanInMinutes}/{monitoredTests}",
                defaults: new { controller = "Results", action = "GetRunningTests", monitoredTests = UrlParameter.Optional });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

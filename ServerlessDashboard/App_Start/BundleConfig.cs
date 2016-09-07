using System.Web;
using System.Web.Optimization;

namespace ServerlessDashboard
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/flot").Include(
                        "~/Scripts/flot/mins/jquery.flot.min.js",
                        "~/Scripts/flot/mins/excanvas.js",
                        "~/Scripts/flot/mins/jquery.colorhelpers.min.js",
                        "~/Scripts/flot/mins/jquery.flot.canvas.min.js",
                        "~/Scripts/flot/mins/jquery.flot.categories.min.js",
                        "~/Scripts/flot/mins/jquery.flot.crosshair.min.js",
                        "~/Scripts/flot/mins/jquery.flot.errorbars.min.js",
                        "~/Scripts/flot/mins/jquery.flot.fillbetween.min.js",
                        "~/Scripts/flot/mins/jquery.flot.image.min.js",
                        "~/Scripts/flot/mins/jquery.flot.navigate.min.js",
                        "~/Scripts/flot/mins/jquery.flot.pie.min.js",
                        "~/Scripts/flot/mins/jquery.flot.resize.min.js",
                        "~/Scripts/flot/mins/jquery.flot.selection.min.js",
                        "~/Scripts/flot/mins/jquery.flot.stack.min.js",
                        "~/Scripts/flot/mins/jquery.flot.symbol.min.js",
                        "~/Scripts/flot/mins/jquery.flot.threshold.min.js",
                        "~/Scripts/flot/mins/jquery.flot.time.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/dateformat").Include(
                        "~/Scripts/dateformat.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServerlessDashboard.Models;
using ServerlessResultManager;

namespace ServerlessDashboard.Controllers
{
    public class HomeController : Controller
    {
        public readonly static int DafaultObservedTimespanInMinutes = 5;

        public ActionResult Index()
        {
            ViewBag.DefaultObservedTimespan = DafaultObservedTimespanInMinutes;
            return View();
        }

        public ActionResult List()
        {
            var repo = new TestRepository();
            var fetchResults = true;
            var tests = repo.GetTestsAfter(DateTime.MinValue, fetchResults: fetchResults);
            tests = tests
                .OrderByDescending(t => t.StartTime)
                .Select(t =>
                {
                    var timeFrame = t.EndTime.HasValue
                        ? (t.EndTime.Value - t.StartTime).TotalMinutes + DafaultObservedTimespanInMinutes
                        : DafaultObservedTimespanInMinutes;
                    return new TestResultsModel(t, (int) timeFrame, fetchResults);
                })
                .ToList();

            return View(tests);
        }

        public ActionResult TestScenarioList()
        {
            var repo = new TestRepository();
            var scenarios = repo.GetTestScenarios(fetchTests: true).OrderByDescending(s => s.StartTimeUtc);
            return View(scenarios);
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
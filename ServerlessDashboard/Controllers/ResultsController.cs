using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using ServerlessDashboard.Models;
using ServerlessResultManager;

namespace ServerlessDashboard.Controllers
{
    public class ResultsController : Controller
    {
        // GET: Results
        [HttpGet]
        public ActionResult GetNewResults(int testId, string startDate)
        {
            var startDateTime = DateTime.Parse(startDate.Replace("!", ":"));
            var repo = new TestRepository();
            var results = repo.GetResultsForTestAfter(testId, startDateTime).ToList();
            var result = ParseResults(results);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetAllResults(int testId)
        {
            var repo = new TestRepository();
            var results = repo.GetTest(testId, fetchResults: true).TestResults;
            var parsedResults = ParseResults(results);
            return Json(parsedResults, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetRunningTests(int timespanInMinutes, string monitoredTests)
        {
            var monitoredTestsIds = new List<int>();

            if (!string.IsNullOrEmpty(monitoredTests))
            {
                var monitoredTestsSplited = monitoredTests.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                monitoredTestsIds = monitoredTestsSplited.Select(x =>
                {
                    var temp = 0;
                    int.TryParse(x, out temp);
                    return temp;
                }).Where(v => v != 0).ToList();

            }

            var startDateTime = DateTime.UtcNow.AddMinutes(-timespanInMinutes);
            var repo = new TestRepository();
            var results = repo.GetTestsAfter(startDateTime).
                Where(t => monitoredTestsIds.All(mt => mt != t.Id)).
                OrderByDescending(x => x.StartTime).
                ToList();
            // no need to pass all results with the test object
            foreach (var result in results)
            {
                result.TestResults = new List<TestResult>();
            }

            var testModels = results.Select(tt => new TestResultsModel(tt, HomeController.DafaultObservedTimespanInMinutes)).ToList();
            return PartialView("TestPartial", testModels);
            //return Json(results, JsonRequestBehavior.AllowGet);
        }


        public ActionResult OpenTests(string ids)
        {
            var idsNumbers = ids.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x =>
            {
                var id = 0L;
                long.TryParse(x, out id);
                return id;
            });

            var repo = new TestRepository();
            var results = repo.GetTestsByIds(idsNumbers).Select(t => new TestResultsModel(t, withResults: true)).ToList();
            return View(results);
        }

        private long ToFlotTimestamp(DateTime timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = timestamp.ToUniversalTime().Subtract(new TimeSpan(epoch.Ticks));
            return (long)(time.Ticks / 10000);
        }
        private Dictionary<string, List<object[]>> ParseResults(ICollection<TestResult> results)
        {
            return new Dictionary<string, List<object[]>>
            {
                { "TotalCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.CallCount }).ToList() },
                { "SuccessCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.SuccessCount }).ToList() },
                { "FailedCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.FailedCount }).ToList() },
                { "TimeoutCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.TimeoutCount }).ToList() },
                { "AverageLatency", results.Where(x => Math.Abs(x.AverageLatency) > 0.01).Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.AverageLatency }).ToList() },
            };
        }
    }
}
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
            var startDateTime = DateTime.SpecifyKind(DateTime.Parse(startDate.Replace("!", ":")),  DateTimeKind.Utc);
            var repo = new TestRepository();
            var results = repo.GetResultsForTestAfter(testId, startDateTime).ToList();
            var result = TestResultsModel.ParseResults(results);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetAllResults(int testId)
        {
            var repo = new TestRepository();
            var results = repo.GetTest(testId, fetchResults: true).TestResults;
            var parsedResults = TestResultsModel.ParseResults(results);
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

        public ActionResult CompareTests(int firstTestId, int secondTestId)
        {
            var repo = new TestRepository();
            var firstTest = repo.GetTest(firstTestId, fetchResults: true);
            var secondTest = repo.GetTest(secondTestId, fetchResults: true);
            var comparisonModel = new TestComparisonModel(firstTest, secondTest);
            return View(comparisonModel);
        }

        public ActionResult CompareScenarios(int firstScenarioId, int secondScenarioId)
        {
            var repo = new TestRepository();
            var firstScenario = repo.GetTestScenario(firstScenarioId, fetchTests: true);
            var secondScenario = repo.GetTestScenario(secondScenarioId, fetchTests: true);
            var comparisonModel = new ScenarioComparisonModel(firstScenario, secondScenario);
            return View(comparisonModel);
        }
    }
}
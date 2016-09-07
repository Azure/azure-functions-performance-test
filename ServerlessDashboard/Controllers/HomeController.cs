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
            var testRepository = new TestRepository();
            var tests = testRepository.GetTestsAfter(DateTime.UtcNow.AddMinutes(-DafaultObservedTimespanInMinutes)).OrderByDescending(x => x.StartTime);
            ViewBag.Tests = tests.Select(t => new TestResultsModel(t, DafaultObservedTimespanInMinutes)).ToList();
            ViewBag.DefaultObservedTimespan = DafaultObservedTimespanInMinutes;
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServerlessResultManager;

namespace ServerlessDashboard.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var testRepository = new TestRepository();
            var tests = testRepository.GetTestsAfter(DateTime.UtcNow.AddDays(-1));
            ViewBag.Tests = tests;
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
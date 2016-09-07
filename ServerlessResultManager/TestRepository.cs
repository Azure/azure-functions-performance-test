using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace ServerlessResultManager
{
    public class TestRepository : ITestRepository
    {
        public TestRepository()
        {
        }

        public Test GetTest(int id)
        {
            using (var model = new ServerlessTestModel())
            {
                return model.Tests.FirstOrDefault(t => t.Id == id);
            }
        }

        public IEnumerable<TestResult> GetResultsForTestAfter(int testId, DateTime startDate)
        {
            using (var model = new ServerlessTestModel())
            {
                return model.TestResults.Where(tr => tr.TestId == testId && tr.Timestamp >= startDate).ToList();
            }
        }

        public IEnumerable<Test> GetTestsAfter(DateTime dateFrom)
        {
            using (var model = new ServerlessTestModel())
            {
                return model.Tests.Where(t => t.StartTime >= dateFrom).Include("TestResults").ToList();
            }
        }

        public Test AddTest(Test test)
        {
            using (var model = new ServerlessTestModel())
            {
                var addedTest = model.Tests.Add(test);
                model.SaveChanges();
                return addedTest;
            }
        }

        public TestResult AddTestResult(Test test, TestResult testResult)
        {
            using (var model = new ServerlessTestModel())
            {
                testResult.TestId = test.Id;
                var addedTestResult = model.TestResults.Add(testResult);
                model.SaveChanges();
                return addedTestResult;
            }
        }

        public void UpdateTest(Test testWithResults)
        {
            using (var model = new ServerlessTestModel())
            {
                model.Tests.AddOrUpdate(testWithResults);
                model.SaveChanges();
            }
        }
    }
}

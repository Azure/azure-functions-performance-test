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

        public Test GetTest(int id, bool fetchResults = false)
        {
            using (var model = new ServerlessTestModel())
            {
                if (fetchResults)
                {
                    return model.Tests.Include("TestResults").FirstOrDefault(t => t.Id == id);
                }
                else
                {
                    return model.Tests.FirstOrDefault(t => t.Id == id);
                }
            }
        }

        public IEnumerable<TestResult> GetResultsForTestAfter(int testId, DateTime startDate)
        {
            using (var model = new ServerlessTestModel())
            {
                return model.TestResults.Where(tr => tr.TestId == testId && tr.Timestamp >= startDate).ToList();
            }
        }

        public IEnumerable<Test> GetTestsAfter(DateTime dateFrom, bool fetchResults = true)
        {
            using (var model = new ServerlessTestModel())
            {
                if (fetchResults)
                {
                    return model.Tests.Where(t => t.StartTime >= dateFrom).Include("TestResults").ToList();
                }
                else
                {
                    return model.Tests.Where(t => t.StartTime >= dateFrom).ToList();
                }

            }
        }

        public IEnumerable<Test> GetTestsByIds(IEnumerable<long> idsNumbers)
        {
            using (var model = new ServerlessTestModel())
            {
                return model.Tests.Where(t => idsNumbers.Contains(t.Id)).Include("TestResults").ToList();
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
                test.TestResults.Add(addedTestResult);
                return addedTestResult;
            }
        }

        public void UpdateTest(Test testWithResults, bool saveResults = false)
        {
            using (var model = new ServerlessTestModel())
            {
                model.Tests.AddOrUpdate(testWithResults);

                if (saveResults)
                {
                    foreach (var result in testWithResults.TestResults)
                    {
                        model.TestResults.AddOrUpdate(result);
                    }
                }

                model.SaveChanges();
            }
        }
    }
}

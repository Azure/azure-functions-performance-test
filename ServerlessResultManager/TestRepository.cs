using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace ServerlessResultManager
{
    public class TestRepository : ITestRepository
    {
        private readonly string _connectionString;

        public TestRepository()
        {
            this._connectionString = ServerlessTestModel.GetConnectionString();
        }

        public TestRepository(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool IsInitialized => !string.IsNullOrEmpty(this._connectionString);

        public Test GetTest(int id, bool fetchResults = false)
        {
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
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
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
            {
                return model.TestResults.Where(tr => tr.TestId == testId && tr.Timestamp >= startDate).ToList();
            }
        }

        public IEnumerable<Test> GetTestsAfter(DateTime dateFrom, bool fetchResults = true)
        {
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
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
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
            {
                return model.Tests.Where(t => idsNumbers.Contains(t.Id)).Include("TestResults").ToList();
            }
        }

        public Test AddTest(Test test)
        {
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
            {
                var addedTest = model.Tests.Add(test);
                model.SaveChanges();
                return addedTest;
            }
        }

        public TestResult AddTestResult(Test test, TestResult testResult)
        {
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return null;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
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
            if (string.IsNullOrEmpty(this._connectionString))
            {
                return;
            }

            using (var model = new ServerlessTestModel(this._connectionString))
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerlessResultManager
{
    public class TestRepository : ITestRepository
    {
        private readonly ServerlessTestModel model;

        public TestRepository()
        {
            model = new ServerlessTestModel();
        }

        public Test GetTest(int id)
        {
            return model.Tests.FirstOrDefault(t => t.Id == id);
        }

        public ICollection<Test> GetTestsAfter(DateTime dateFrom)
        {
            return model.Tests.Include("TestResults").Where(t => t.StartTime >= dateFrom).ToList();
        }

        public Test AddTest(Test test)
        {
            return model.Tests.Add(test);
        }

        public TestResult AddTestResult(Test test, TestResult testResult)
        {
            testResult.Test = test;
            return model.TestResults.Add(testResult);
        }

        public void SaveChanges()
        {
            model.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    model.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

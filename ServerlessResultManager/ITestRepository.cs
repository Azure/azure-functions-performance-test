
using System;
using System.Collections.Generic;

namespace ServerlessResultManager
{
    public interface ITestRepository
    {
        Test GetTest(int id);

        ICollection<Test> GetTestsAfter(DateTime dateFrom);

        Test AddTest(Test test);

        TestResult AddTestResult(Test test, TestResult testResult);
        void UpdateTest(Test testWithResults);
    }
}


using System;
using System.Collections.Generic;

namespace ServerlessResultManager
{
    public interface ITestRepository
    {
        Test GetTest(int id);

        IEnumerable<Test> GetTestsAfter(DateTime dateFrom);

        Test AddTest(Test test);

        TestResult AddTestResult(Test test, TestResult testResult);

        void UpdateTest(Test testWithResults);

        IEnumerable<TestResult> GetResultsForTestAfter(int testId, DateTime startDate);
    }
}

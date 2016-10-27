
using System;
using System.Collections.Generic;

namespace ServerlessResultManager
{
    public interface ITestRepository
    {
        bool IsInitialized { get; }
        Test GetTest(int id, bool fetchResults);

        IEnumerable<Test> GetTestsAfter(DateTime dateFrom, bool fetchResults);

        Test AddTest(Test test);

        TestResult AddTestResult(Test test, TestResult testResult);

        void UpdateTest(Test testWithResults, bool saveResults);

        IEnumerable<TestResult> GetResultsForTestAfter(int testId, DateTime startDate);

        IEnumerable<Test> GetTestsByIds(IEnumerable<long> idsNumbers);
    }
}

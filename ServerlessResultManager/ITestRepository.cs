
using System;
using System.Collections.Generic;

namespace ServerlessResultManager
{
    public interface ITestRepository
    {
        bool IsInitialized { get; }
        Test GetTest(long id, bool fetchResults);

        TestScenario GetTestScenario(int id, bool fetchTests);

        IEnumerable<Test> GetTestsAfter(DateTime dateFrom, bool fetchResults);

        Test AddTest(Test test);

        TestResult AddTestResult(Test test, TestResult testResult);

        TestScenario AddTestScenario(TestScenario testsScenario);

        void UpdateTest(Test testWithResults, bool saveResults);

        IEnumerable<TestResult> GetResultsForTestAfter(int testId, DateTime startDate);

        IEnumerable<TestScenario> GetTestScenarios(bool fetchTests);

        IEnumerable<Test> GetTestsByIds(IEnumerable<long> idsNumbers);

        void UpdateTestScenario(TestScenario scenario);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using ServerlessResultManager;

namespace ServerlessDashboard.Models
{
    public class TestComparisonModel
    {
        public TestResultsModel FirstTest { get; set; }
        public TestResultsModel SecondTest { get; set; }
        public List<object> ComparedThroughput { get; set; }
        public List<object> ComparedLatency { get; set; }
        public List<object> ComparedErrorCount { get; set; }
        public List<object> ComparedTimeoutCount { get; set; }
        public List<object> ComparedHostConcurrencyCount { get; set; }
        public string StartTimeDiff => (FirstTest.StartTime - SecondTest.StartTime).TotalSeconds.ToString();

        public string EndTimeDiff
        {
            get
            {
                var diff = (FirstTest.EndTime - SecondTest.EndTime);
                return diff.HasValue ? diff.Value.TotalSeconds.ToString() : string.Empty;
            }
        }

        public string TotalExecutionRequestsDiff => (FirstTest.TotalExecutionRequests - SecondTest.TotalExecutionRequests).ToString();
        public string PlatformDiff => FirstTest.Platform == SecondTest.Platform ? "Same" : "Different";
        public string SucceededExecutionsDiff => (FirstTest.SucceededExecutions - SecondTest.SucceededExecutions).ToString();
        public string FailedExecutionsDiff => (FirstTest.FailedExecutions - SecondTest.FailedExecutions).ToString();
        public string TimeoutExecutionsDiff => (FirstTest.TimeoutExecutions - SecondTest.TimeoutExecutions).ToString();
        public string AverageLatencyDiff => (FirstTest.AverageLatency - SecondTest.AverageLatency).ToString();
        public string TargetEpsDiff => (FirstTest.TargetEps - SecondTest.TargetEps).ToString();
        public string ExecutionCountDiff => (FirstTest.ExecutionCount - SecondTest.ExecutionCount).ToString();
        public string ExecutionTimeStdDiff=> (FirstTest.ExecutionTimeStandardDeviation - SecondTest.ExecutionTimeStandardDeviation).ToString();
        public string FunctionClockTimeDiff => (FirstTest.FunctionClockTime - SecondTest.FunctionClockTime).ToString();
        public string HostConcurrencyDiff => (FirstTest.HostConcurrency - SecondTest.HostConcurrency).ToString();
        public string ThroughputDiff => (FirstTest.Throughput - SecondTest.Throughput).ToString();
        public string ErrorsDiff => (FirstTest.Errors - SecondTest.Errors).ToString();

        public TestComparisonModel(Test firstTest, Test secondTest)
        {
            FirstTest = new TestResultsModel(firstTest);
            SecondTest = new TestResultsModel(secondTest);
            ComparedThroughput = GetResults((x) => x.SuccessCount, "Throughput");
            ComparedLatency = GetResults((x) => x.AverageLatency, "Latency");
            ComparedErrorCount = GetResults((x) => x.FailedCount, "Errors");
            ComparedTimeoutCount = GetResults((x) => x.TimeoutCount, "Timeouts");
            ComparedHostConcurrencyCount = GetResults((x) => x.HostConcurrency, "HostConcurrency");
        }

        private List<object> GetResults(Func<TestResult, object> selector, string prefixName)
        {
            var firstResults = this.FirstTest.TestResults;
            var secondResults = this.SecondTest.TestResults;
            var timeDiff = (firstResults.First().Timestamp - secondResults.First().Timestamp).TotalSeconds;

            return new List<object>
            {
                new
                {
                    label = prefixName + this.FirstTest.Name.Substring(8) + this.FirstTest.Id,
                    data = firstResults.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), selector(x) }).ToList()
                },
                new
                {
                    label = prefixName + this.FirstTest.Name.Substring(8) + this.SecondTest.Id,
                    data = secondResults.Select(x => new object[] { ToFlotTimestamp(x.Timestamp.Add(TimeSpan.FromSeconds(timeDiff))), selector(x) }).ToList()
                }
            };
        }

        private static long ToFlotTimestamp(DateTime timestamp)
        {
            timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = timestamp.ToUniversalTime().Subtract(new TimeSpan(epoch.Ticks));
            return (long)(time.Ticks / 10000);
        }
    }
}
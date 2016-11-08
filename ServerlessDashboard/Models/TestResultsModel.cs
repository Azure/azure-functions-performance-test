using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServerlessResultManager;

namespace ServerlessDashboard.Models
{
    public class TestResultsModel : Test
    {
        public long TotalExecutionRequests { get; set; }
        public long SucceededExecutions { get; set; }
        public long FailedExecutions { get; set; }
        public long TimeoutExecutions { get; set; }
        public int ViewTimespanInMinutes { get; set; }
        public double AverageLatency { get; set; }

        public TestResultsModel(Test test, int viewTimeSpanInMinutes = 10, bool withResults = true)
        {
            if (withResults)
            {
                this.TotalExecutionRequests = test.TestResults.Sum(r => r.CallCount);
                this.SucceededExecutions = test.TestResults.Sum(r => r.SuccessCount);
                this.FailedExecutions = test.TestResults.Sum(r => r.FailedCount);
                this.TimeoutExecutions = test.TestResults.Sum(r => r.TimeoutCount);
                this.AverageLatency = test.TestResults.Any() ? test.TestResults.Average(r => r.AverageLatency) : 0.0;
                this.TestResults = test.TestResults.OrderBy(t => t.Timestamp).ToList();
            }
            else
            {
                this.TestResults = new List<TestResult>();
            }

            this.Id = test.Id;
            this.Name = test.Name;
            this.Description = test.Description;
            this.StartTime = test.StartTime;
            this.EndTime = test.EndTime;
            this.Platform = test.Platform;
            this.Owner = test.Owner;
            this.AverageExecutionTime = test.AverageExecutionTime;
            this.ExecutionCount = test.ExecutionCount;
            this.ExecutionTimeStandardDeviation = test.ExecutionTimeStandardDeviation;
            this.FunctionClockTime = test.FunctionClockTime;
            this.HostConcurrency = test.HostConcurrency;
            this.Throughput = test.Throughput;
            this.Errors = test.Errors;
            this.TargetEps = test.TargetEps;
            ViewTimespanInMinutes = test.EndTime.HasValue ? (int)(test.EndTime.Value - test.StartTime).TotalMinutes : viewTimeSpanInMinutes;
        }

        public static Dictionary<string, List<object[]>> ParseResults(ICollection<TestResult> results)
        {
            return new Dictionary<string, List<object[]>>
            {
                { "TotalCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.CallCount }).ToList() },
                { "SuccessCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.SuccessCount }).ToList() },
                { "FailedCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.FailedCount }).ToList() },
                { "TimeoutCount", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.TimeoutCount }).ToList() },
                { "AverageLatency", results.Where(x => Math.Abs(x.AverageLatency) > 0.01).Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.AverageLatency }).ToList() },
                { "HostConcurrency", results.Select(x => new object[] { ToFlotTimestamp(x.Timestamp), x.HostConcurrency }).ToList() }
            };
        }

        // transfer datetime into format undestandable for flot library
        public static long ToFlotTimestamp(DateTime timestamp)
        {
            timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = timestamp.ToUniversalTime().Subtract(new TimeSpan(epoch.Ticks));
            return (long)(time.Ticks / 10000);
        }
    }
}
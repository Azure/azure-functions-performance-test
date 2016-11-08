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
                TotalExecutionRequests = test.TestResults.Sum(r => r.CallCount);
                SucceededExecutions = test.TestResults.Sum(r => r.SuccessCount);
                FailedExecutions = test.TestResults.Sum(r => r.FailedCount);
                TimeoutExecutions = test.TestResults.Sum(r => r.TimeoutCount);
                AverageLatency = test.TestResults.Any() ? test.TestResults.Average(r => r.AverageLatency) : 0.0;
                TestResults = test.TestResults;
            }
            else
            {
                TestResults = new List<TestResult>();
            }

            Id = test.Id;
            Name = test.Name;
            Description = test.Description;
            StartTime = test.StartTime;
            EndTime = test.EndTime;
            Platform = test.Platform;
            Owner = test.Owner;
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
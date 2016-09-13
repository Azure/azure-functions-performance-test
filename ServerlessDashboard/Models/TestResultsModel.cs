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
            ViewTimespanInMinutes = test.EndTime.HasValue ? (int)(test.EndTime.Value - test.StartTime).TotalMinutes : viewTimeSpanInMinutes;
        }
    }
}
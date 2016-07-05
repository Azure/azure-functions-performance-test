using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServerlessBenchmark.MetricInfo;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class AzurePerformanceResultProviderBase:PerfResultProvider
    {
        private List<FunctionLogs.AzureFunctionLogs> _logs;
        
        [PerfMetric("Average Execution Time")]
        protected TimeSpan? CalculateAverageExecutionTime(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = executionTimes.Average(e => e.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(avgExecutionTime);
        }

        [PerfMetric("Execution Time Standard Deviation")]
        protected string CalculateExecutionTimeStandardDeviation(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = CalculateAverageExecutionTime(functionName, testStartTime, testEndTime, expectedExecutionCount).GetValueOrDefault().TotalMilliseconds;
            var sumOfSquaredDifferences =
                executionTimes.Select(t => (t.TotalMilliseconds - avgExecutionTime) * (t.TotalMilliseconds - avgExecutionTime)).Sum();
            var std = Math.Sqrt(sumOfSquaredDifferences / executionTimes.Count());
            return std.ToString();
        }

        [PerfMetric("Execution Count")]
        protected int CalculateExecutionCount(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var executionCount = executionTimes.Count();
            return executionCount;
        }

        [PerfMetric(PerfMetrics.FunctionClockTime)]
        protected TimeSpan? CalculateFunctionClockTime(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var firstTime = logs.OrderBy(log => log.StartTime).First().StartTime;
            var endTime = logs.OrderBy(log => log.EndTime).Last().EndTime;
            var clockTime = endTime - firstTime;
            return TimeSpan.FromMilliseconds(clockTime.TotalMilliseconds);
        }

        private IEnumerable<FunctionLogs.AzureFunctionLogs> _functionLogs;
        private IEnumerable<FunctionLogs.AzureFunctionLogs> FunctionLogs(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutions)
        {
            if (_logs == null)
            {
                _logs = PerfResultProviders.FunctionLogs.GetAzureFunctionLogs(functionName, testStartTime, testEndTime, true, expectedExecutions);                
            }
            return _logs;
        }

        private IEnumerable<TimeSpan>RetrieveExecutionTimes(IEnumerable<FunctionLogs.AzureFunctionLogs> logs)
        {
            var executionTimes = logs.Select(l => TimeSpan.FromMilliseconds((l.EndTime - l.StartTime).Milliseconds));
            return executionTimes;
        }

        public override PerfTestResult GetPerfMetrics(string functionName, DateTime testStartTime, DateTime testEndTime,
            string inputTriggerName = null, string outputTriggerName = null, int expectedExecutionCount = 0)
        {
            var perfResults = new PerfTestResult();
            var perfCalculatingMethods = GetType().GetRuntimeMethods().Where(m => m.IsDefined(typeof(PerfMetric)));
            foreach (var method in perfCalculatingMethods)
            {
                var result = method.Invoke(this, new object[] { functionName, testStartTime, testEndTime, expectedExecutionCount}).ToString();
                var perfMetricAttribute = method.GetCustomAttribute(typeof(PerfMetric)) as PerfMetric;
                var metricName = perfMetricAttribute.MetricName;
                perfResults.AddMetric(metricName, result);
            }

            var additionalPerfResults = ObtainAdditionalPerfMetrics(perfResults, functionName, testStartTime, testEndTime) ?? new Dictionary<string, string>();
            var additionalPerfResultsList = additionalPerfResults.ToList();
            foreach (var additionalPerfResult in additionalPerfResultsList)
            {
                perfResults.AddMetric(additionalPerfResult.Key, additionalPerfResult.Value);
            }
            return perfResults;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServerlessBenchmark.MetricInfo;
using System.IO;
using System.Text;
using Amazon.CloudWatchLogs.Model;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class AzurePerformanceResultProviderBase:PerfResultProvider
    {
        private List<FunctionLogs.AzureFunctionLogs> _logs;
        
        [PerfMetric(PerfMetrics.AverageExecutionTime)]
        protected TimeSpan? CalculateAverageExecutionTime(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = executionTimes.Average(e => e.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(avgExecutionTime);
        }

        [PerfMetric(PerfMetrics.ExecutionTimeStandardDeviation)]
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

        [PerfMetric(PerfMetrics.ExecutionCount)]
        protected int CalculateExecutionCount(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var executionCount = executionTimes.Count();
            return executionCount;
        }

        [PerfMetric(PerfMetrics.Throughput)]
        protected double CalculateThroughput(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);

            var grouped =
                from l in logs
                where l.RawStatus == "CompletedSuccess"
                group l by TrimMilliseconds(l.EndTime) into g
                select new { TimeStamp = g.Key, Count = g.Count() };           

            var actualStartTime = grouped.Min(x => x.TimeStamp);
            var actualEndTime = grouped.Max(x => x.TimeStamp);
            var countByTimestamp = grouped.ToDictionary(x => x.TimeStamp, x => x.Count);

            var stringBuffer = new StringBuilder();
            var throughputList = new List<int>();

            for (var timeStamp = actualStartTime; timeStamp < actualEndTime; timeStamp = timeStamp.AddSeconds(1))
            {
                int count = 0;
                countByTimestamp.TryGetValue(timeStamp, out count);
                throughputList.Add(count);
                stringBuffer.AppendFormat("{0},{1}{2}", count, timeStamp, Environment.NewLine);                
            }
            var fileName = string.Format("{0}-Throughput.txt", Guid.NewGuid().ToString());
            File.WriteAllText(fileName, stringBuffer.ToString());

            return throughputList.Average();
        }

        [PerfMetric(PerfMetrics.ThroughputGraph)]
        
        protected string GenerateThroughputGraph(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            var secondsInGroup = 15;
            var logGroupped = GetAverageLogCountInTimeWindow(logs.ToList(), secondsInGroup);
            var fileName = string.Format("Azure-{0}-Throughput-graph.pdf", Guid.NewGuid().ToString());
            PrintThroughputGraph(logGroupped, fileName, secondsInGroup);
            return string.Format("Plot can be found at {0}", fileName);
        }

        [PerfMetric(PerfMetrics.HostConcurrency)]
        protected double CalculateHostConcurrency(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime, expectedExecutionCount);
            const int concurrentTimeWindowInSeconds = 15;
            var processedLogs = new List<TrimmedFunctionLog>();
            var concurrencyList = new List<int>();
            foreach (var log in logs)
            {
                processedLogs.Add(new TrimmedFunctionLog
                {
                    Timestamp = TrimMilliseconds(log.EndTime),
                    ContainerName = log.ContainerName
                });
            }

            var stringBuffer = new StringBuilder();

            var orderedLogs = processedLogs.OrderBy(l => l.Timestamp);
            var actualStartTime = orderedLogs.First().Timestamp;
            var actualEndTime = orderedLogs.Last().Timestamp;
            for (var timeStamp = actualStartTime; timeStamp < actualEndTime; timeStamp = timeStamp.AddSeconds(1))
            {
                var concurrency = processedLogs.Where(l => l.Timestamp > timeStamp.AddSeconds(-1 * concurrentTimeWindowInSeconds)
                && l.Timestamp < timeStamp.AddSeconds(concurrentTimeWindowInSeconds)).Select(c => c.ContainerName).Distinct().Count();
                concurrencyList.Add(concurrency);
                stringBuffer.AppendFormat("{0},{1}{2}", concurrency, timeStamp, Environment.NewLine);
            }
            var fileName = string.Format("{0}-HostConcurrency.txt", Guid.NewGuid().ToString());
            File.WriteAllText(fileName, stringBuffer.ToString());

            return concurrencyList.Average();
        }

        private class TrimmedFunctionLog
        {
            public DateTime Timestamp { get; set; }
            public string ContainerName { get; set; }
        }

        private Dictionary<DateTime, double> GetAverageLogCountInTimeWindow(List<FunctionLogs.AzureFunctionLogs> logs, int windowTimespanInSeconds)
        {
            var orderedLogs = logs.OrderBy(l => l.Timestamp);
            var actualStartTime = orderedLogs.First().Timestamp;
            var actualEndTime = orderedLogs.Last().Timestamp;
            var result = new Dictionary<DateTime, double>();

            for (var timeStamp = actualStartTime; timeStamp < actualEndTime; timeStamp = timeStamp.AddSeconds(windowTimespanInSeconds))
            {
                var startTime = timeStamp.AddSeconds(-1 * windowTimespanInSeconds);
                var logsCount = logs.Count(
                    l => l.Timestamp > startTime &&
                         l.Timestamp <= timeStamp.AddSeconds(windowTimespanInSeconds));
                result[startTime.DateTime.AddSeconds(windowTimespanInSeconds / 2)] = logsCount;
            }

            return result;
        }

        private static DateTime TrimMilliseconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
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

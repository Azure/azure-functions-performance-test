using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServerlessBenchmark.MetricInfo;
using System.IO;
using System.Text;
using Amazon.CloudWatchLogs.Model;
using System.Diagnostics;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class AzurePerformanceResultProviderBase:PerfResultProvider
    {
        private List<FunctionLogs.AzureFunctionLogs> _logs;
        private string _azureStorageConnectionString;

        public AzurePerformanceResultProviderBase(string azureStorageConnectionString)
        {
            _azureStorageConnectionString = azureStorageConnectionString;
        }
        
        [PerfMetric(PerfMetrics.AverageExecutionTime)]
        protected TimeSpan? CalculateAverageExecutionTime(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = executionTimes.Average(e => e.TotalMilliseconds);

            if (this.DatabaseTest != null)
            {
                UpdateResultsWithLatency(this.DatabaseTest, RetrieveAvgExecutionTimesPerSecond(logs));
                this.DatabaseTest.AverageExecutionTime = avgExecutionTime * 1000;
            }

            return TimeSpan.FromMilliseconds(avgExecutionTime);
        }

        [PerfMetric(PerfMetrics.ExecutionTimeStandardDeviation)]
        protected string CalculateExecutionTimeStandardDeviation(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = CalculateAverageExecutionTime(functionName, testStartTime, testEndTime, expectedExecutionCount).GetValueOrDefault().TotalMilliseconds;
            var sumOfSquaredDifferences =
                executionTimes.Select(t => (t.TotalMilliseconds - avgExecutionTime) * (t.TotalMilliseconds - avgExecutionTime)).Sum();
            var std = Math.Sqrt(sumOfSquaredDifferences / executionTimes.Count());

            if (this.DatabaseTest != null)
            {
                this.DatabaseTest.ExecutionTimeStandardDeviation = std;
            }

            return std.ToString();
        }

        [PerfMetric(PerfMetrics.ExecutionCount)]
        protected int CalculateExecutionCount(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var executionTimes = RetrieveExecutionTimes(logs);
            var executionCount = executionTimes.Count();

            if (this.DatabaseTest != null)
            {
                this.DatabaseTest.ExecutionCount = executionCount;
            }

            return executionCount;
        }

        [PerfMetric("Total Errors")]
        protected int TotalErrors(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var totalFailed = logs.Count(l => l.RawStatus == "CompletedFailure");

            if (this.DatabaseTest != null)
            {
                this.DatabaseTest.Errors = totalFailed;
            }

            return totalFailed;
        }

        [PerfMetric(PerfMetrics.Throughput)]
        protected double CalculateThroughput(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);

            var grouped =
                from l in logs
                group l by TrimMilliseconds(l.EndTime) into g
                select new { TimeStamp = g.Key, Count = g.Count() };

            var actualStartTime = grouped.Min(x => x.TimeStamp);
            var actualEndTime = grouped.Max(x => x.TimeStamp);
            var countByTimestamp = grouped.ToDictionary(x => x.TimeStamp, x => x.Count);

            if (actualEndTime - actualStartTime > TimeSpan.FromHours(24))
            {
                throw new Exception(string.Format("Aborting calculation. The max time range is 24 hours. StartTime: {0}, EndTime: {1}.", actualStartTime, actualEndTime));
            }

            var throughputList = new List<int>();
            var fileName = string.Format("{0}/{1}-Throughput.txt", functionName, Guid.NewGuid().ToString());
            Directory.CreateDirectory(functionName);

            using (var logWriter = new StreamWriter(fileName))
            {
                for (var timeStamp = actualStartTime; timeStamp <= actualEndTime; timeStamp = timeStamp.AddSeconds(1))
                {
                    int count = 0;
                    countByTimestamp.TryGetValue(timeStamp, out count);
                    throughputList.Add(count);
                    logWriter.WriteLine("{0},{1}", count, timeStamp);
                }
            }

            Debug.Assert(throughputList.Sum() == countByTimestamp.Values.Sum(), "We missed log counts somehow");
            var averageThroughput = throughputList.Average();

            if (this.DatabaseTest != null)
            {
                this.DatabaseTest.Throughput = averageThroughput;
            }

            return averageThroughput;
        }

        [PerfMetric(PerfMetrics.ThroughputGraph)]
        
        protected string GenerateThroughputGraph(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var secondsInGroup = 15;
            var logGroupped = GetAverageLogCountInTimeWindow(logs.ToList(), secondsInGroup);
            var fileName = string.Format("{0}/Azure-{1}-Throughput-graph.pdf", functionName, Guid.NewGuid().ToString());
            Directory.CreateDirectory(functionName);
            PrintThroughputGraph(logGroupped, fileName, secondsInGroup);            

            return string.Format("Plot can be found at {0}", fileName);
        }

        [PerfMetric(PerfMetrics.HostConcurrency)]
        protected double CalculateHostConcurrency(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
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

            var orderedLogs = processedLogs.OrderBy(l => l.Timestamp);
            var actualStartTime = orderedLogs.First().Timestamp;
            var actualEndTime = orderedLogs.Last().Timestamp;

            if (actualEndTime - actualStartTime > TimeSpan.FromHours(24))
            {
                throw new Exception(string.Format("Aborting calculation. The max time range is 24 hours. StartTime: {0}, EndTime: {1}.", actualStartTime, actualEndTime));
            }

            var fileName = string.Format("{0}/{1}-HostConcurrency.txt", functionName, Guid.NewGuid().ToString());
            Directory.CreateDirectory(functionName);
            var hostConcurrencyInTime = new Dictionary<DateTime, int>();

            using (var logWriter = new StreamWriter(fileName))
            {
                for (var timeStamp = actualStartTime; timeStamp < actualEndTime; timeStamp = timeStamp.AddSeconds(1))
                {
                    var concurrency = processedLogs.Where(l => l.Timestamp > timeStamp.AddSeconds(-1 * concurrentTimeWindowInSeconds)
                    && l.Timestamp < timeStamp.AddSeconds(concurrentTimeWindowInSeconds)).Select(c => c.ContainerName).Distinct().Count();
                    concurrencyList.Add(concurrency);
                    logWriter.WriteLine("{0},{1}", concurrency, timeStamp);
                    hostConcurrencyInTime[timeStamp] = concurrency;
                }
            }

            var averageHostConcurrency = concurrencyList.Average();

            if (this.DatabaseTest != null)
            {
                UpdateResultsWithHostConcurrency(this.DatabaseTest, hostConcurrencyInTime);
                this.DatabaseTest.HostConcurrency = averageHostConcurrency;
            }

            return averageHostConcurrency;
        }

        private class TrimmedFunctionLog
        {
            public DateTime Timestamp { get; set; }
            public string ContainerName { get; set; }
        }

        private Dictionary<DateTime, double> GetAverageLogCountInTimeWindow(List<FunctionLogs.AzureFunctionLogs> logs, int windowTimespanInSeconds)
        {
            var grouped =
                from l in logs                
                group l by RoundByTimeSpan(l.EndTime, TimeSpan.FromSeconds(windowTimespanInSeconds)) into g
                select new { TimeStamp = g.Key, Count = (double)g.LongCount() };

            var actualStartTime = grouped.Min(x => x.TimeStamp);
            var actualEndTime = grouped.Max(x => x.TimeStamp);
            var countByTimestamp = grouped.ToDictionary(x => x.TimeStamp, x => x.Count);            

            if(actualEndTime - actualStartTime > TimeSpan.FromDays(1))
            {
                throw new Exception(string.Format("Aborting calculation. There is a problem with the timerange. StartTime: {0}, EndTime: {1}.", actualStartTime, actualEndTime));                
            }

            var result = new Dictionary<DateTime, double>();

            for (var timeStamp = actualStartTime; timeStamp <= actualEndTime; timeStamp = timeStamp.AddSeconds(windowTimespanInSeconds))
            {
                double count = 0;
                countByTimestamp.TryGetValue(timeStamp, out count);
                result[timeStamp] = count;
            }

            Debug.Assert(result.Values.Sum() == countByTimestamp.Values.Sum(), "We missed log counts somehow");

            return result;
        }

        private static DateTime TrimMilliseconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
        }

        private static DateTime RoundByTimeSpan(DateTime dt, TimeSpan ts)
        {
            return dt.AddTicks(-(dt.Ticks % ts.Ticks));
        }


        [PerfMetric(PerfMetrics.FunctionClockTime)]
        protected TimeSpan? CalculateFunctionClockTime(string functionName, DateTime testStartTime, DateTime testEndTime, int expectedExecutionCount)
        {
            var logs = FunctionLogs(functionName, testStartTime, expectedExecutionCount);
            var firstTime = logs.OrderBy(log => log.StartTime).First().StartTime;
            var endTime = logs.OrderBy(log => log.EndTime).Last().EndTime;
            var clockTime = endTime - firstTime;
            var functionClockTime = TimeSpan.FromMilliseconds(clockTime.TotalMilliseconds);

            if (this.DatabaseTest != null)
            {
                this.DatabaseTest.FunctionClockTime = functionClockTime.TotalSeconds;
            }

            return functionClockTime;
        }

        private IEnumerable<FunctionLogs.AzureFunctionLogs> _functionLogs;
        private IEnumerable<FunctionLogs.AzureFunctionLogs> FunctionLogs(string functionName, DateTime testStartTime, int expectedExecutions)
        {
            if (_logs == null)
            {
                _logs = PerfResultProviders.FunctionLogs.GetAzureFunctionLogs(functionName, testStartTime, _azureStorageConnectionString, expectedExecutions);                
            }
            return _logs;
        }

        private IEnumerable<TimeSpan> RetrieveExecutionTimes(IEnumerable<FunctionLogs.AzureFunctionLogs> logs)
        {
            var executionTimes = logs.Select(l => TimeSpan.FromMilliseconds((l.EndTime - l.StartTime).Milliseconds));
            return executionTimes;
        }

        private IDictionary<DateTime, double> RetrieveAvgExecutionTimesPerSecond(IEnumerable<FunctionLogs.AzureFunctionLogs> logs)
        {
            var grouped =
                from l in logs
                group l by TrimMilliseconds(l.EndTime) into g
                select new { TimeStamp = g.Key, Average = g.Average(l => (l.EndTime - l.StartTime).Milliseconds) };

            return grouped.ToDictionary(x => x.TimeStamp, key => key.Average);
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

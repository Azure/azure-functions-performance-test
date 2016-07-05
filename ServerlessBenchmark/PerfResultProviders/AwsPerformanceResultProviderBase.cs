using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using ServerlessBenchmark.MetricInfo;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class AwsPerformanceResultProviderBase:PerfResultProvider
    {
        private List<OutputLogEvent> _logs; 
        private List<OutputLogEvent> FunctionLogs(string functionName, DateTime start, DateTime end)
        {
            if (_logs == null)
            {
                using (var cwClient = new AmazonCloudWatchLogsClient())
                {
                    var logStreams = new List<LogStream>();
                    DescribeLogStreamsResponse lResponse;
                    string nextToken = null;
                    do
                    {
                        lResponse =
                            cwClient.DescribeLogStreams(new DescribeLogStreamsRequest("/aws/lambda/" + functionName)
                            {
                                NextToken = nextToken
                            });
                        logStreams.AddRange(lResponse.LogStreams);
                    } while (!string.IsNullOrEmpty(nextToken = lResponse.NextToken));
                    List<OutputLogEvent> logs = new List<OutputLogEvent>();
                    logStreams.ForEach(
                        s =>
                            cwClient.GetLogEvents(new GetLogEventsRequest("/aws/lambda/" + functionName,
                                s.LogStreamName)
                            {
                                Limit = 10000
                            }).Events.ForEach(e => logs.Add(e)));
                    _logs = logs;
                }                
            }
            return _logs;
        }

        private IEnumerable<TimeSpan?> RetrieveExecutionTimes(List<OutputLogEvent> logs)
        {
            return RetrieveTimes(logs, "Billed");
        }

        private IEnumerable<TimeSpan?> RetrieveTimes(List<OutputLogEvent> logs, string timeType)
        {
            ConcurrentBag<TimeSpan?> executionTimes = new ConcurrentBag<TimeSpan?>();
            Parallel.ForEach(logs, log =>
            {
                if (log.Message.ToLower().Contains("report"))
                {
                    var executionTimeStringInMs = Regex.Matches(log.Message, String.Format("{0} Duration:(?<executiontime>.*)ms", timeType))[0].Groups["executiontime"].Captures[0].Value;
                    var ts = TimeSpan.FromMilliseconds(double.Parse(executionTimeStringInMs));
                    executionTimes.Add(ts);
                }
            });
            return executionTimes;
        } 

        [PerfMetric(PerfMetrics.FunctionClockTime)]
        protected TimeSpan? CalculateFunctionClockTime(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var orderedLogs = logs.OrderBy(log => log.IngestionTime);
            var clockTime = orderedLogs.Last().IngestionTime - orderedLogs.First().IngestionTime;
            return TimeSpan.FromMilliseconds(clockTime.TotalMilliseconds);
        }

        [PerfMetric("Execution Time Standard Deviation")]
        protected string CalculateExecutionTimeStandardDeviation(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = CalculateAverageExecutionTime(functionName, testStartTime, testEndTime).GetValueOrDefault().TotalMilliseconds;
            var sumOfSquaredDifferences =
                executionTimes.Select(t => (t.GetValueOrDefault().TotalMilliseconds - avgExecutionTime)*(t.GetValueOrDefault().TotalMilliseconds - avgExecutionTime)).Sum();
            var std = Math.Sqrt(sumOfSquaredDifferences/executionTimes.Count());
            return std.ToString();
        }

        [PerfMetric("Average Execution Time")]
        protected TimeSpan? CalculateAverageExecutionTime(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = executionTimes.Average(e => e.GetValueOrDefault().TotalMilliseconds);
            return TimeSpan.FromMilliseconds(avgExecutionTime);
        }

        private TimeSpan? CalculateAggregateComputeTime(List<OutputLogEvent> logs)
        {
            var executionTimes = RetrieveExecutionTimes(logs);
            var aggregateComputeTime = executionTimes.Sum(e => e.GetValueOrDefault().TotalMilliseconds);
            return TimeSpan.FromMilliseconds(aggregateComputeTime);
        }

        private double CalculateCompression(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionCount = CalculateExecutionCount(functionName, testStartTime, testEndTime);
            var aggregateExecutionTime = CalculateAggregateComputeTime(logs);
            double scaleFactor = aggregateExecutionTime.GetValueOrDefault().TotalMilliseconds/executionCount;
            return scaleFactor;
        }

        [PerfMetric("Execution Count")]
        protected int CalculateExecutionCount(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionTimes = RetrieveExecutionTimes(logs);
            var executionCount = executionTimes.Count();
            return executionCount;
        }

        protected abstract Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime, DateTime testEndTime, List<OutputLogEvent> lambdaExecutionLogs);

        protected override Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime,
            DateTime testEndTime)
        {
            return ObtainAdditionalPerfMetrics(genericPerfTestResult, functionName, testStartTime, testEndTime, FunctionLogs(functionName, testStartTime, testEndTime));
        }
    }
}

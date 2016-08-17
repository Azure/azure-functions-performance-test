using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
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

        [PerfMetric(PerfMetrics.ExecutionTimeStandardDeviation)]
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

        [PerfMetric(PerfMetrics.ExecutionCount)]
        protected int CalculateExecutionCount(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionTimes = RetrieveExecutionTimes(logs);
            var executionCount = executionTimes.Count();
            return executionCount;
        }

        [PerfMetric(PerfMetrics.AverageExecutionTime)]
        protected TimeSpan? CalculateAverageExecutionTime(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var executionTimes = RetrieveExecutionTimes(logs);
            var avgExecutionTime = executionTimes.Average(e => e.GetValueOrDefault().TotalMilliseconds);
            return TimeSpan.FromMilliseconds(avgExecutionTime);
        }

        [PerfMetric(PerfMetrics.HostConcurrency)]
        protected string CalculateHostUsedAtMaximum(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime);
            var logByMinute = logs.GroupBy(l => (l.Timestamp - TimeSpan.FromMilliseconds(l.Timestamp.Millisecond)));

            var sum = 0;
            foreach (var logGroup in logByMinute)
            {
                var hosts = RetrieveHostNames(logGroup.ToList());
                sum += hosts.Distinct().Count();
            }

            return (sum / logByMinute.Count()).ToString();
        }

        [PerfMetric(PerfMetrics.Throughput)]
        protected string CalculateThroughput(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime).Where(l => l.Message.Contains("Duration:"));
            var logByMinute = GetAverageLogCountInTimeWindow(logs.ToList(), 1);
            var stringBuffer = new StringBuilder();

            foreach (var log in logByMinute.OrderBy(l => l.Key))
            {
                stringBuffer.AppendFormat("{0},{1}{2}", log, log.Key, Environment.NewLine);
            }

            var fileName = string.Format("AWS-{0}-Throughput.txt", Guid.NewGuid().ToString());
            File.WriteAllText(fileName, stringBuffer.ToString());

            return logByMinute.Select(l => l.Value).Average().ToString();
        }

        [PerfMetric(PerfMetrics.ThroughputGraph)]
        protected string CalculateThroughputGraph(string functionName, DateTime testStartTime, DateTime testEndTime)
        {
            var logs = FunctionLogs(functionName, testStartTime, testEndTime).Where(l => l.Message.Contains("Duration:"));
            var secondsInGroup = 15;
            var logGroupped = GetAverageLogCountInTimeWindow(logs.ToList(), secondsInGroup);
            var fileName = string.Format("AWS-{0}-Throughput-graph.pdf", Guid.NewGuid().ToString());
            PrintThrouputGraph(logGroupped, fileName, secondsInGroup);
            return string.Format("Plot can be found at {0}", fileName);
        }

        private IEnumerable<string> RetrieveHostNames(List<OutputLogEvent> logs)
        {
            var hostNames = new ConcurrentBag<string>();
            Parallel.ForEach(logs, log =>
            {
                if (log.Message.ToLower().Contains("machine name"))
                {
                    var hostName = Regex.Matches(log.Message, "machine name (?<machinename>.*)")[0].Groups["machinename"].Captures[0].Value;
                    hostNames.Add(hostName);
                }
            });
            return hostNames.ToList();
        }

        private Dictionary<DateTime, double> GetAverageLogCountInTimeWindow(List<OutputLogEvent> logs, int windowTimespanInSeconds)
        {
            var orderedLogs = logs.OrderBy(l => l.Timestamp);
            var actualStartTime = orderedLogs.First().Timestamp;
            var actualEndTime = orderedLogs.Last().Timestamp;
            var result = new Dictionary<DateTime, double>();

            for (var timeStamp = actualStartTime; timeStamp < actualEndTime; timeStamp = timeStamp.AddSeconds(windowTimespanInSeconds))
            {
                var startTime = timeStamp.AddSeconds(-1*windowTimespanInSeconds);
                var logsCount = logs.Count(
                    l => l.Timestamp > startTime &&
                         l.Timestamp <= timeStamp.AddSeconds(windowTimespanInSeconds));
                result[startTime.AddSeconds(windowTimespanInSeconds / 2)] = logsCount;
            }

            return result;
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

        protected abstract Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime, DateTime testEndTime, List<OutputLogEvent> lambdaExecutionLogs);

        protected override Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime,
            DateTime testEndTime)
        {
            return ObtainAdditionalPerfMetrics(genericPerfTestResult, functionName, testStartTime, testEndTime, FunctionLogs(functionName, testStartTime, testEndTime));
        }
    }
}

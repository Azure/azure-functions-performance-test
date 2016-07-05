using System;
using System.Collections.Generic;
using Amazon.CloudWatchLogs.Model;
using ServerlessBenchmark.MetricInfo;

namespace ServerlessBenchmark.PerfResultProviders
{
    public sealed class AwsGenericPerformanceResultsProvider:AwsPerformanceResultProviderBase
    {
        protected override Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult,
            string functionName, DateTime testStartTime, DateTime testEndTime, List<OutputLogEvent> lambdaExecutionLogs)
        {
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServerlessBenchmark.MetricInfo;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class PerfResultProvider
    {

        protected abstract Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime, DateTime testEndTime);
        
        public virtual PerfTestResult GetPerfMetrics(string functionName, DateTime testStartTime, DateTime testEndTime,string inputTriggerName = null, string outputTriggerName = null, int expectedExecutionCount = 0)
        {
            var perfResults = new PerfTestResult();
            var perfCalculatingMethods = GetType().GetRuntimeMethods().Where(m => m.IsDefined(typeof (PerfMetric)));
            foreach (var method in perfCalculatingMethods)
            {
                var result = method.Invoke(this, new object[]{functionName, testStartTime, testEndTime}).ToString();
                var perfMetricAttribute = method.GetCustomAttribute(typeof (PerfMetric)) as PerfMetric;
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

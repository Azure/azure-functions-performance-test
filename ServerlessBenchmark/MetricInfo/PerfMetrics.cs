using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessBenchmark.MetricInfo
{
    public enum PerfMetrics
    {
        ExecutionTimeStandardDeviation,
        ExecutionCount,
        FunctionClockTime,
        AverageExecutionTime,
        Throughput,
        HostConcurrency,
        ThroughputGraph
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PerfMetric : Attribute
    {
        public string MetricName { get; private set; }
        public PerfMetric(PerfMetrics metric)
        {
            MetricName = Enum.GetName(typeof (PerfMetrics), metric);
        }

        public PerfMetric(string metricName)
        {
            MetricName = metricName;
        }
    }
}

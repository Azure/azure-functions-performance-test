using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerlessBenchmark.MetricInfo;

namespace ServerlessBenchmark
{
    public class PerfTestResult : SortedList<string, string>
    {
        public void AddMetric(string metricName, string val)
        {
            Add(metricName, val);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var list = this.ToList();
            foreach (var metric in list)
            {
                sb.AppendFormat("{0}      {1}\n", metric.Key, metric.Value);
            }
            return sb.ToString();
        }
    }
}

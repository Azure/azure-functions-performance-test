using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerlessBenchmark.PerfResultProviders
{
    public class AzureGenericPerformanceResultsProvider : AzurePerformanceResultProviderBase
    {
        protected override Dictionary<string, string> ObtainAdditionalPerfMetrics(PerfTestResult genericPerfTestResult, string functionName, DateTime testStartTime,
            DateTime testEndTime)
        {
            return null;
        }
    }
}

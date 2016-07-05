using System;
using System.Collections.Generic;

namespace ServerlessBenchmark
{
    sealed internal class SupplementalPerfTestResult:PerfTestResult
    {
        private Dictionary<string, string> _supplementalPerformanceTestResults; 
        internal Dictionary<string, string> SupplementalPerformanceTestResults {
            get
            {
                if (_supplementalPerformanceTestResults == null)
                {
                    _supplementalPerformanceTestResults = new Dictionary<string, string>();
                }
                return _supplementalPerformanceTestResults;
            }
        }

        public override string ToString()
        {
            var perfResults = base.ToString();
            foreach (var metric in SupplementalPerformanceTestResults)
            {
                perfResults += String.Format("{0}      {1}\n", metric.Key, metric.Value);
            }
            return perfResults;
        }
    }
}

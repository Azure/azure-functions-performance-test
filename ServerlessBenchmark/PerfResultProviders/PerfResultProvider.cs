using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ServerlessBenchmark.MetricInfo;
using ServerlessResultManager;

namespace ServerlessBenchmark.PerfResultProviders
{
    public abstract class PerfResultProvider
    {
        public Test DatabaseTest { get; set; }

        protected void PrintThroughputGraph(Dictionary<DateTime, double> data, string fileName, int timeResolutionInSeconds)
        {
            var model = new PlotModel { Title = "Throughput in time (items finished/second)" };
            var timeAxis = new DateTimeAxis
            {
                StringFormat = "hh:mm:ss"
            };

            model.Axes.Add(timeAxis);
            var series = new LineSeries();

            foreach (var log in data.OrderBy(l => l.Key))
            {                
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(log.Key), log.Value / timeResolutionInSeconds));
            }

            model.Series.Add(series);

            using (var stream = File.Create(fileName))
            {
                var pdfExporter = new PdfExporter { Width = 600, Height = 400 };
                pdfExporter.Export(model, stream);
            }
        }

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

        protected void UpdateResultsWithHostConcurrency(Test dbTest, Dictionary<DateTime, int> results, bool force = false)
        {
            var toleranceInSeconds = 5;

            foreach (var result in dbTest.TestResults)
            {
                if (force || result.HostConcurrency == null)
                {
                    var time = result.Timestamp;
                    var closestInfo = results
                        .Where(x => x.Key >= time && (x.Key - time) <= TimeSpan.FromSeconds(toleranceInSeconds))
                        .OrderBy(x => x.Key);

                    if (closestInfo.Any())
                    {
                        result.HostConcurrency = closestInfo.First().Value;
                    }
                }
            }
        }

        protected void UpdateResultsWithLatency(Test dbTest, IDictionary<DateTime, double> results, bool force = false)
        {
            var toleranceInSeconds = 5;

            foreach (var result in dbTest.TestResults.Where(r => r != null))
            {
                if (force || Math.Abs(result.AverageLatency) < 0.00001)
                {
                    var time = result.Timestamp;
                    var closestInfo = results
                        .Where(x => x.Key >= time && (x.Key - time) <= TimeSpan.FromSeconds(toleranceInSeconds))
                        .OrderBy(x => x.Key);

                    if (closestInfo.Any())
                    {
                        result.AverageLatency = closestInfo.First().Value;
                    }
                }
            }
        }
    }
}

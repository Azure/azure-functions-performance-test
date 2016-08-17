using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.PerfResultProviders;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    /// <summary>
    /// Base HTTP function that all platform HTTP type functions can inherit from
    /// </summary>
    public abstract class HttpTriggerTest : FunctionTest
    {
        protected override sealed IEnumerable<string> SourceItems { get; set; }
        private int _totalSuccessRequestsPerSecond;
        private int _totalFailedRequestsPerSecond;
        private int _totalTimedOutRequestsPerSecond;
        private int _totalActiveRequests;
        private int _totalLatency;
        private int _totalRequestsPerSecond;

        protected abstract bool Setup();

        protected HttpTriggerTest(string functionName, string[] urls):base(functionName)
        {
            if (!urls.Any())
            {
                throw new ArgumentException("Urls is empty", "urls");
            }
            if (String.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be empty", "functionName");
            }

            SourceItems = urls.ToList();
        }

        protected override Task TestWarmup()
        {
            var warmSite = SourceItems.First();
            var client = new HttpClient();
            var cs = new CancellationTokenSource(Constants.HttpTriggerTimeoutMilliseconds);
            HttpResponseMessage response;
            try
            {
                response = client.GetAsync(warmSite, cs.Token).Result;
            }
            catch (TaskCanceledException)
            {
                throw new Exception(String.Format("Warm up passed timeout of {0}ms", Constants.HttpTriggerTimeoutMilliseconds));
            }

            var isSuccessfulSetup = response.IsSuccessStatusCode;
            return Task.FromResult(isSuccessfulSetup);
        }

        protected override bool TestSetupWithRetry()
        {
            return Setup();
        }

        protected abstract override PerfResultProvider PerfmormanceResultProvider { get; }

        protected override Task PreReportGeneration(DateTime testStartTime, DateTime testEndTime)
        {
            return Task.FromResult(0);
        }

        protected async override Task Load(IEnumerable<string> requestItems)
        {
            await LoadSites(requestItems);
        }

        private async Task LoadSites(IEnumerable<string> sites)
        {
            var client = new HttpClient();
            var loadRequests = new List<Task>();
            foreach (var site in sites)
            {
                var t = Task.Run(async () =>
                {
                    try
                    {
                        var cs = new CancellationTokenSource(Constants.HttpTriggerTimeoutMilliseconds);
                        var request = client.GetAsync(site, cs.Token);
                        var requestSent = DateTime.Now;
                        Interlocked.Increment(ref _totalActiveRequests);
                        var response = await request;
                        var responseReceived = DateTime.Now;
                        Interlocked.Decrement(ref _totalActiveRequests);
                        Interlocked.Add(ref _totalLatency, (int)(responseReceived - requestSent).TotalMilliseconds);
                        Interlocked.Increment(ref _totalRequestsPerSecond);
                        if (response.IsSuccessStatusCode)
                        {
                            _totalSuccessRequestsPerSecond += 1;
                        }
                        else
                        {
                            _totalFailedRequestsPerSecond += 1;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _totalTimedOutRequestsPerSecond += 1;
                    }
                });
                loadRequests.Add(t);
            }
            await Task.WhenAll(loadRequests);
        }

        protected override IDictionary<string, string> CurrentTestProgress()
        {
            var testProgressData = base.CurrentTestProgress();
            testProgressData.Add("Success", _totalSuccessRequestsPerSecond.ToString());
            testProgressData.Add("Failed", _totalFailedRequestsPerSecond.ToString());
            testProgressData.Add("Timeout", _totalTimedOutRequestsPerSecond.ToString());
            testProgressData.Add("Active", _totalActiveRequests.ToString());
            testProgressData.Add("AvgLatency(ms)", (_totalLatency / (_totalRequestsPerSecond != 0 ? _totalRequestsPerSecond : 1)).ToString());

            //reset values
            ResetHttpCounters();

            return testProgressData;
        }

        private void ResetHttpCounters()
        {
            _totalFailedRequestsPerSecond = 0;
            _totalSuccessRequestsPerSecond = 0;
            _totalTimedOutRequestsPerSecond = 0;
            _totalLatency = 0;
            _totalRequestsPerSecond = 0;
        }
    }
}

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
        private const int TimeoutInMilliseconds = 30 * 1000;
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
            var cs = new CancellationTokenSource(TimeoutInMilliseconds);
            HttpResponseMessage response;
            try
            {
                response = client.GetAsync(warmSite, cs.Token).Result;
            }
            catch (TaskCanceledException)
            {
                throw new Exception(String.Format("Warm up passed timeout of {0}ms", TimeoutInMilliseconds));
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
                //var t = client.GetAsync(site, cs.Token);
                var t = Task.Run(async () =>
                {
                    try
                    {
                        var cs = new CancellationTokenSource(TimeoutInMilliseconds);
                        await client.GetAsync(site, cs.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        //ignore
                    }
                });
                loadRequests.Add(t);
            }
            await Task.WhenAll(loadRequests);
        }
    }
}

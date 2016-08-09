using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    /// <summary>
    /// Base HTTP function that all platform HTTP type functions can inherit from
    /// </summary>
    public abstract class HttpTriggerTest : FunctionTest
    {
        private List<string> SourceItems { get; set; }
        protected string FunctionName { get; private set; }
        private int _exepectedTotalExecutions;
        private const int TimeoutInMilliseconds = 30 * 1000;

        protected HttpTriggerTest(string functionName, string[] urls)
        {
            if (!urls.Any())
            {
                throw new ArgumentException("Urls is empty", "urls");
            }
            if (String.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be empty", "functionName");
            }

            FunctionName = functionName;
            SourceItems = urls.ToList();
        }

        private async Task<bool> SetUp(int retries = 3)
        {
            var warmSite = SourceItems.First();
            var client = new HttpClient();
            var cs = new CancellationTokenSource(TimeoutInMilliseconds);
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(warmSite, cs.Token);
            }
            catch (TaskCanceledException)
            {
                throw new Exception(String.Format("Warm up passed timeout of {0}ms", TimeoutInMilliseconds));
            }

            bool isSuccessfulSetup = response.IsSuccessStatusCode;
            if (!response.IsSuccessStatusCode)
            {
                do
                {
                    retries = retries - 1;
                    isSuccessfulSetup = await SetUp(retries);
                } while (retries > 0 && !isSuccessfulSetup);
            }

            return isSuccessfulSetup && TestSetup();
        }

        protected abstract override bool TestSetup();
        protected abstract override PerfResultProvider PerfmormanceResultProvider { get; }

        public override async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            if (warmup)
            {
                if (!await SetUp())
                {
                    throw new Exception("Setup failed");   
                }
            }

            var startTime = DateTime.Now;
            await loadProfile.ExecuteRateAsync(LoadSites);
            loadProfile.Dispose();
            var endTime = DateTime.Now;
            var perfResult = PerfmormanceResultProvider.GetPerfMetrics(FunctionName, startTime, endTime, expectedExecutionCount: _exepectedTotalExecutions);
            return perfResult;
        }

        private async Task LoadSites(int targetNumberOfSites)
        {
            int srcNumberOfItems = SourceItems.Count();
            IEnumerable<string> selectedItems;
            var randomizedSites = SourceItems.OrderBy(i => Guid.NewGuid());
            if (targetNumberOfSites <= srcNumberOfItems)
            {
                selectedItems = randomizedSites.Take(targetNumberOfSites);
            }
            else
            {
                var tmpList = new List<string>();
                do
                {
                    tmpList.AddRange(randomizedSites.Take(targetNumberOfSites));
                    targetNumberOfSites -= srcNumberOfItems;
                } while (targetNumberOfSites >= 0);
                selectedItems = tmpList;
            }
            Console.WriteLine("EPS = {0} {1}", selectedItems.Count(), DateTime.Now);
            Interlocked.Add(ref _exepectedTotalExecutions, selectedItems.Count());
            await LoadSites(selectedItems);
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

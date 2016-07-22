using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Edm;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureHttpTriggerTest : IFunctionsTest
    {
        private List<string> SourceItems { get; set; }
        private string _functionName;
        private int _exepectedTotalExecutions;
        private const int TimeoutInMilliseconds = 30 * 1000;

        private PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(); }
        }
 
        public AzureHttpTriggerTest(string functionName, string[] urls)
        {
            if (!urls.Any())
            {
                throw new ArgumentException("Urls is empty", "urls");
            }
            if (String.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be empty", "functionName");
            }

            _functionName = functionName;
            SourceItems = urls.ToList();
        }

        private async Task<bool> SetUp(int retries = 3)
        {
            var warmSite = SourceItems.First();
            var client = new HttpClient();
            var cs = new CancellationTokenSource(TimeoutInMilliseconds);
            try
            {
                var response = await client.GetAsync(warmSite, cs.Token);
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                throw new Exception(String.Format("Warm up passed timeout of {0}ms", TimeoutInMilliseconds));
            }
        }

        public async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            if (warmup)
            {
                if (await SetUp())
                {
                    DateTime startTime, endTime;
                    startTime = DateTime.Now;
                    await loadProfile.ExecuteRateAsync(LoadSites);
                    loadProfile.Dispose();
                    endTime = DateTime.Now;
                    var perfResult = PerfmormanceResultProvider.GetPerfMetrics(_functionName, startTime, endTime, expectedExecutionCount: _exepectedTotalExecutions);
                    return perfResult;
                }
            }
            return null;
        }

        private async Task LoadSites(int targetNumberOfSites)
        {
            int srcNumberOfItems = SourceItems.Count();
            Console.WriteLine(targetNumberOfSites);
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
            Console.WriteLine("EPS = {0} {1}", randomizedSites.Count(), DateTime.Now);
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

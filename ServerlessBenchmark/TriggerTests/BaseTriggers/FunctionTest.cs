using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class FunctionTest
    {
        protected string FunctionName { get; set; }
        protected abstract IEnumerable<string> SourceItems { get; set; }
        protected int ExpectedExecutionCount;
        protected abstract bool TestSetupWithRetry();
        protected abstract Task TestWarmup();
        protected abstract Task PreReportGeneration(DateTime testStartTime, DateTime testEndTime);
        protected abstract ICloudPlatformController CloudPlatformController { get; }
        protected abstract PerfResultProvider PerfmormanceResultProvider { get; }

        protected FunctionTest(string functionName)
        {
            FunctionName = functionName;
        }

        public async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            var retries = 3;
            bool isSuccessSetup;
            do
            {
                isSuccessSetup = TestSetupWithRetry();
                retries = retries - 1;
            } while (retries > 0 && !isSuccessSetup);

            if (warmup)
            {
                await TestWarmup();
            }

            Console.WriteLine("--START-- Running load");
            var startTime = DateTime.Now;
            var sw = Stopwatch.StartNew();
            await loadProfile.ExecuteRateAsync(GenerateLoad);
            loadProfile.Dispose();
            sw.Stop();
            var clientEndTime = DateTime.Now;
            Console.WriteLine("--END-- Elapsed time:      {0}", sw.Elapsed);
            await PreReportGeneration(startTime, clientEndTime);
            var perfResult = PerfmormanceResultProvider.GetPerfMetrics(FunctionName, startTime, clientEndTime, expectedExecutionCount: ExpectedExecutionCount);
            return perfResult;
        }

        protected abstract Task Load(IEnumerable<string> requestItems);

        protected async Task GenerateLoad(int requests)
        {
            var srcNumberOfItems = SourceItems.Count();
            List<string> selectedItems;
            var randomResources = SourceItems.OrderBy(i => Guid.NewGuid());
            if (requests <= srcNumberOfItems)
            {
                selectedItems = randomResources.Take(requests).ToList();
            }
            else
            {
                var tmpList = new List<string>();
                do
                {
                    tmpList.AddRange(randomResources.Take(requests));
                    requests -= srcNumberOfItems;
                } while (requests >= 0);
                selectedItems = tmpList;
            }
            Console.WriteLine("EPS = {0} {1}", selectedItems.Count(), DateTime.Now);
            Interlocked.Add(ref ExpectedExecutionCount, selectedItems.Count());
            await Load(selectedItems);
        }
    }
}

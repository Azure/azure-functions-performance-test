using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class StorageTriggerTest : IFunctionsTest
    {
        private int _expectedDestinationBlobContainerCount;
        protected abstract string StorageType { get; }
        protected string FunctionName { get; set; }
        protected abstract List<CloudPlatformResponse> CleanUpStorageResources();
        protected abstract bool VerifyTargetDestinationStorageCount(int expectedCount);
        protected List<string> SourceItems { get; set; }
        protected abstract bool TestSetup();
        protected abstract Task UploadItems(IEnumerable<string> items);
        protected abstract ICloudPlatformController CloudPlatformController { get; }
        protected abstract PerfResultProvider PerfmormanceResultProvider { get; }

        protected StorageTriggerTest(string functionName, string[] items)
        {
            FunctionName = functionName;
            SourceItems = items.ToList();
        }

        private bool SetUp(int retries = 3)
        {
            bool successfulSetup;
            Console.WriteLine("{0} trigger tests - setup", StorageType);
            try
            {
                Console.WriteLine("Deleting storage items");
                var cloudPlatformResponses = CleanUpStorageResources();
                var undoneJobs =
                    cloudPlatformResponses.Where(
                        response => response != null && response.HttpStatusCode != HttpStatusCode.OK);
                successfulSetup = !undoneJobs.Any() && TestSetup();
                if (!successfulSetup && retries > 0)
                {
                    retries = retries - 1;
                    successfulSetup = SetUp(retries);
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not setup Test"), e);
            }
            return successfulSetup;
        }

        private async Task TestWarmUp()
        {
            Console.WriteLine("{0} Trigger Warmup - Starting", StorageType);

            var sw = Stopwatch.StartNew();

            await UploadItems(new[] { SourceItems.First() });

            Console.WriteLine("{0} Trigger Warmup - Verify test", StorageType);

            bool isWarmUpSuccess = VerifyTargetDestinationStorageCount(1);

            sw.Stop();

            Console.WriteLine("{0} Trigger Warmup - Clean Up", StorageType);

            SetUp();

            Console.WriteLine(isWarmUpSuccess ? "Warmup - Done!" : "Warmup - Done with failures");
            Console.WriteLine("{1} Trigger Warmup - Elapsed Time: {0}ms", sw.ElapsedMilliseconds, StorageType);
        }

        public async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            int blobCount = SourceItems.Count();
            DateTime clientStartTime, clientEndTime;

            if (SetUp())
            {
                if (warmup)
                {
                    await TestWarmUp();
                }

                Console.WriteLine("Posting Storage Items");
                clientStartTime = DateTime.Now;
                var sw = Stopwatch.StartNew();
                await loadProfile.ExecuteRateAsync(UploadStorageItems);
                loadProfile.Dispose();
                sw.Stop();
                Console.WriteLine("Elapsed time to post items:      {0}", sw.Elapsed);
            }
            else
            {
                throw new Exception("Could not successfully setup Test");
            }

            Console.WriteLine("Verify all items are there:");
            VerifyTargetDestinationStorageCount(_expectedDestinationBlobContainerCount);

            clientEndTime = DateTime.Now;
            var perfResult = PerfmormanceResultProvider.GetPerfMetrics(FunctionName, clientStartTime, clientEndTime, expectedExecutionCount: _expectedDestinationBlobContainerCount);
            return perfResult;
        }

        private async Task UploadStorageItems(int targetNumberOfItems)
        {
            int srcNumberOfItems = SourceItems.Count();
            IEnumerable<string> selectedItems;
            if (targetNumberOfItems <= srcNumberOfItems)
            {
                selectedItems = SourceItems.Take(targetNumberOfItems);
            }
            else
            {
                var tmpList = new List<string>();
                do
                {
                    tmpList.AddRange(SourceItems.Take(targetNumberOfItems));
                    targetNumberOfItems -= srcNumberOfItems;
                } while (targetNumberOfItems >= 0);
                selectedItems = tmpList;
            }
            Console.WriteLine("EPS = {0} {1}", selectedItems.Count(), DateTime.Now);
            await UploadItems(selectedItems);
            Interlocked.Add(ref _expectedDestinationBlobContainerCount, selectedItems.Count());
        }
    }
}

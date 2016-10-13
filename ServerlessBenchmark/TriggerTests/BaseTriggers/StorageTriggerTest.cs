using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class StorageTriggerTest : FunctionTest
    {
        protected abstract string StorageType { get; }
        protected override sealed IEnumerable<string> SourceItems { get; set; }
        protected abstract List<CloudPlatformResponse> CleanUpStorageResources();
        protected abstract Task<bool> VerifyTargetDestinationStorageCount(int expectedCount);
        protected abstract bool Setup();
        protected abstract Task UploadItems(IEnumerable<string> items);
        protected abstract override ICloudPlatformController CloudPlatformController { get; }
        protected abstract override PerfResultProvider PerfmormanceResultProvider { get; }

        protected StorageTriggerTest(string functionName, string[] items):base(functionName)
        {
            FunctionName = functionName;
            SourceItems = items.ToList();
        }

        protected override bool TestSetupWithRetry()
        {
            Logger.LogInfo("{0} trigger tests - setup", StorageType);
            bool successfulSetup;
            try
            {
                Logger.LogInfo("Deleting storage items");
                var cloudPlatformResponses = CleanUpStorageResources();
                var undoneJobs =
                    cloudPlatformResponses.Where(
                        response => response != null && response.HttpStatusCode != HttpStatusCode.OK);
                successfulSetup = !undoneJobs.Any() && Setup();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not setup Test"), e);
            }
            return successfulSetup;            
        }

        protected async override Task TestWarmup()
        {
            Logger.LogInfo("{0} Trigger Warmup - Starting", StorageType);

            var sw = Stopwatch.StartNew();

            await UploadItems(new[] { SourceItems.First() });

            Logger.LogInfo("{0} Trigger Warmup - Verify test", StorageType);

            bool isWarmUpSuccess = await VerifyTargetDestinationStorageCount(1);

            sw.Stop();

            Logger.LogInfo("{0} Trigger Warmup - Clean Up", StorageType);

            TestSetupWithRetry();

            Logger.LogInfo(isWarmUpSuccess ? "Warmup - Done!" : "Warmup - Done with failures");
            Logger.LogInfo("{1} Trigger Warmup - Elapsed Time: {0}ms", sw.ElapsedMilliseconds, StorageType);
        }

        protected async override Task PreReportGeneration(DateTime testStartTime, DateTime testEndTime)
        {
            var expectedDestinationBlobContainerCount = ExpectedExecutionCount;
            await VerifyTargetDestinationStorageCount(expectedDestinationBlobContainerCount);
        }

        protected override async Task Load(IEnumerable<string> requestItems)
        {
            await UploadItems(requestItems);
        }
    }
}

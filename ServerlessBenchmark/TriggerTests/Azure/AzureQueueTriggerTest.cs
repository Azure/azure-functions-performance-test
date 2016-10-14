using System.Configuration;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureQueueTriggerTest:QueueTriggerTest
    {
        public AzureQueueTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, string[] messages, string sourceQueue, string targetQueue) : base(functionName, warmUpTimeInMinutes, eps, messages, sourceQueue, targetQueue)
        {
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AzureController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider { DatabaseTest = this.TestWithResults }; }
        }

        protected override bool Setup()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName,
                ConfigurationManager.AppSettings["AzureStorageConnectionString"]);
        }
    }
}

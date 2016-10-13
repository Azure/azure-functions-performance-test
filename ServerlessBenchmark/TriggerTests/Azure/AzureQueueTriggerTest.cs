using System.Configuration;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureQueueTriggerTest:QueueTriggerTest
    {
        public AzureQueueTriggerTest(string functionName, string[] messages, string sourceQueue, string targetQueue) : base(functionName, messages, sourceQueue, targetQueue)
        {
        }

        protected override void SaveCurrentProgessToDb()
        {
            //skip
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AzureController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(); }
        }

        protected override bool Setup()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName,
                ConfigurationManager.AppSettings["AzureStorageConnectionString"],
                this.Logger);
        }
    }
}

using System.Configuration;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureQueueTriggerTest:QueueTriggerTest
    {
        private string _azureStorageConnectionString;

        public AzureQueueTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, string[] messages, 
            string sourceQueue, string targetQueue, string azureStorageConnectionString = null) : base(functionName, eps, warmUpTimeInMinutes, messages, sourceQueue, targetQueue)
        {
            _azureStorageConnectionString = azureStorageConnectionString;
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get
            {
                return new AzureController(_azureStorageConnectionString)
                {
                    Logger = this.Logger
                };
            }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider { DatabaseTest = this.TestWithResults }; }
        }

        protected override bool Setup()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName,
                ConfigurationManager.AppSettings["AzureStorageConnectionString"],
                this.Logger);
        }
    }
}

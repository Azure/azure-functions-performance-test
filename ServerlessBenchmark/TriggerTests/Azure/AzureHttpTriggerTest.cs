using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureHttpTriggerTest:HttpTriggerTest
    {
        private string _azureStorageConnectionString;

        public AzureHttpTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, IEnumerable<string> urls, string azureStorageConnectionString) : base(functionName, eps, warmUpTimeInMinutes, urls.ToArray())
        {
            _azureStorageConnectionString = azureStorageConnectionString;
        }

        protected override bool Setup()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName, _azureStorageConnectionString, this.Logger);
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get
            {
                return new AzureController
                {
                    Logger = this.Logger
                };
            }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(_azureStorageConnectionString) { DatabaseTest = this.TestWithResults }; }
        }
    }
}

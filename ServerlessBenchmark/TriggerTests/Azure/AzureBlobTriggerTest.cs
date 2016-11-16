using System.Collections.Generic;
using System.Linq;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureBlobTriggerTest:BlobTriggerTest
    {
        private string _azureStorageConnectionString;

        public AzureBlobTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, IEnumerable<string> blobs, string inputContainer,
            string outputContainer, string azureStorageConnectionString) : base(functionName, eps, warmUpTimeInMinutes, blobs.ToArray(), inputContainer, outputContainer)
        {
            _azureStorageConnectionString = azureStorageConnectionString;
        }

        protected override bool Setup()
        {
            return RemoveAzureFunctionLogs();
        }

        protected override void SaveCurrentProgessToDb()
        {
            // skip
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
            get { return new AzureGenericPerformanceResultsProvider(_azureStorageConnectionString) { DatabaseTest = this.TestWithResults }; }
        }

        private bool RemoveAzureFunctionLogs()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName, _azureStorageConnectionString, this.Logger);
        }
    }
}

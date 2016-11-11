using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureBlobTriggerTest:BlobTriggerTest
    {
        private string _azureStorageConnectionStringConfigName;

        public AzureBlobTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, IEnumerable<string> blobs, string inputContainer,
            string outputContainer, string azureStorageConnectionStringConfigName = null) : base(functionName, eps, warmUpTimeInMinutes, blobs.ToArray(), inputContainer, outputContainer)
        {
            _azureStorageConnectionStringConfigName = azureStorageConnectionStringConfigName;
        }

        protected override bool Setup()
        {
            return RemoveAzureFunctionLogs() && EnableLoggingIfDisabled();
        }

        protected override void SaveCurrentProgessToDb()
        {
            // skip
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get
            {
                return new AzureController(_azureStorageConnectionStringConfigName)
                {
                    Logger = this.Logger
                };
            }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(_azureStorageConnectionStringConfigName) { DatabaseTest = this.TestWithResults }; }
        }

        private bool RemoveAzureFunctionLogs()
        {
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                var operationContext = new OperationContext();
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var logs = FunctionLogs.GetAzureFunctionLogs(FunctionName);
                var table = tableClient.GetTableReference(Utility.GetCurrentLogsTableName());
                logs.ForEach(entity => table.Execute(TableOperation.Delete(entity)));
                return true;
            }
            return false;
        }

        private bool EnableLoggingIfDisabled()
        {
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var operationContext = new OperationContext();
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var currentServiceProperties = blobClient.GetServiceProperties();
            return true;
        }
    }
}

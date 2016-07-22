using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests
{
    public class AzureBlobTriggerTest:BlobTriggerTest
    {
        public AzureBlobTriggerTest(string functionName, IEnumerable<string> blobs, string inputContainer,
            string outputContainer) : base(functionName, blobs.ToArray(), inputContainer, outputContainer)
        {
            
        }

        public AzureBlobTriggerTest(string functionName, IEnumerable<string> blobs)
        {
            //todo find the input and output container given the container name
        }

        protected override bool TestSetup()
        {
            return RemoveAzureFunctionLogs() && EnableLoggingIfDisabled();
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AzureController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(); }
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
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
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

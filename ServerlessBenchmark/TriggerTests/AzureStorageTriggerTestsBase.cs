using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests
{
    public abstract class AzureStorageTriggerTestsBase: IFunctionsTest
    {
        private CloudBlobClient BlobClient { get; set; }
        private CloudQueueClient QueueClient { get; set; }
        private CloudTableClient TableClient { get; set; }

        protected string BlobContainer { get; set; }
        protected string Queue { get; set; }

        protected AzureStorageTriggerTestsBase(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            //set the default concurrent connection limit to something higher
            var servicePoints = new Uri[]{storageAccount.QueueEndpoint, storageAccount.BlobEndpoint};
            foreach (Uri serviceEndpoint in servicePoints)
            {
                var servicePoint = ServicePointManager.FindServicePoint(serviceEndpoint);
                servicePoint.UseNagleAlgorithm = false;
                servicePoint.ConnectionLimit = 15000;
                servicePoint.Expect100Continue = false;   
            }

            BlobClient = storageAccount.CreateCloudBlobClient();
            QueueClient = storageAccount.CreateCloudQueueClient();
            TableClient = storageAccount.CreateCloudTableClient();
        }

        public PerfTestResult Run(TestRequest request, ICloudPlatformController cloudPlatform)
        {
            throw new NotImplementedException();
        }

        public PerfTestResult Run(ICloudPlatformController cloudPlatform)
        {
            throw new NotImplementedException();
        }

        public Task<PerfTestResult> RunAsync(ICloudPlatformController cloudPlatform)
        {
            throw new NotImplementedException();
        }

        public PerfTestResult Run()
        {
            throw new NotImplementedException();
        }

        public PerfTestResult Run(bool warmup = true)
        {
            throw new NotImplementedException();
        }
    }
}

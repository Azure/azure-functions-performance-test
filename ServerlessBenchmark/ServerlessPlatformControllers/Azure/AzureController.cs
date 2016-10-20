using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServerlessBenchmark.ServerlessPlatformControllers.Azure
{
    public class AzureController:ICloudPlatformController
    {
        private readonly CloudStorageAccount storageAccount;
        
        public Platform PlatformName => Platform.Azure;

        private CloudQueueClient QueueClient
        {
            get { return storageAccount.CreateCloudQueueClient(); }
        }

        private CloudBlobClient BlobClient
        {
            get { return storageAccount.CreateCloudBlobClient(); }
        }

        public AzureController()
        {
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
                var queueEndpoint = ServicePointManager.FindServicePoint(storageAccount.QueueEndpoint);
                var blobEndpoint = ServicePointManager.FindServicePoint(storageAccount.BlobEndpoint);
                var endpoints = new List<ServicePoint>
                {
                    {queueEndpoint},
                    {blobEndpoint}
                };
                foreach (var endpoint in endpoints)
                {
                    endpoint.UseNagleAlgorithm = false;
                    endpoint.ConnectionLimit = 15000;
                    endpoint.Expect100Continue = false;
                }

            }
            else
            {
                throw new Exception("AzureStorageConnectionString does not exist in app config");
            }
        }
        
        public CloudPlatformResponse PostMessage(CloudPlatformRequest request)
        {
            throw new NotImplementedException("Use this for event hub");
        }

        public CloudPlatformResponse PostMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException("Use this for event hub");
        }

        public Task<CloudPlatformResponse> PostMessagesAsync(CloudPlatformRequest request)
        {
            throw new NotImplementedException("Use this for event hub");
        }

        public CloudPlatformResponse GetMessage(CloudPlatformRequest request)
        {
            throw new NotImplementedException("Use this for event hub");
        }

        public CloudPlatformResponse GetMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException("Use this for event hub");
        }

        public CloudPlatformResponse DeleteMessages(CloudPlatformRequest request)
        {            
            var client = storageAccount.CreateCloudQueueClient();            
            var queue = client.GetQueueReference(request.Source);
            queue.Clear();
            return new CloudPlatformResponse { HttpStatusCode = HttpStatusCode.OK };                                   
        }

        public async Task<CloudPlatformResponse> EnqueueMessagesAsync(CloudPlatformRequest request)
        {
            var response = new CloudPlatformResponse();
            var messages = request.Data[Constants.Message] as IEnumerable<string>;
            var queue = QueueClient.GetQueueReference(request.Source);
            var tasks = new List<Task>();
            var operationResultsByTask = new Dictionary<int, OperationContext>();

            foreach (var message in messages)
            {
                var operationContext = new OperationContext();
                var t = queue.AddMessageAsync(new CloudQueueMessage(message), null, null, null, operationContext);
                tasks.Add(t);
                operationResultsByTask.Add(t.Id, operationContext);
            }

            await Task.WhenAll(tasks);
            var operationResults = operationResultsByTask.Values;
            var successfulPost = operationResults.All(operationContext => operationContext.LastResult.HttpStatusCode == 201);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
        }

        public async Task<CloudPlatformResponse> DequeueMessagesAsync(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            
            var queue = QueueClient.GetQueueReference(request.Source);
            var messages = await queue.GetMessagesAsync(Constants.MaxDequeueAmount);            

            var messagesString = messages.Select(message => message.AsString);
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            response.Data = messagesString;
            return response;
        }

        public async Task<CloudPlatformResponse> GetOutputItemsCount(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var queue = QueueClient.GetQueueReference(request.Source);
            await queue.FetchAttributesAsync();
            var queueLength = queue.ApproximateMessageCount;
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            response.Data = queueLength;
            return response;
        }

        public CloudPlatformResponse PostBlob(CloudPlatformRequest request)
        {
            OperationContext uploadContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var client = storageAccount.CreateCloudBlobClient();
            var blobContainer = client.GetContainerReference(request.Source);
            var blob = blobContainer.GetBlockBlobReference(request.Key);
            blob.UploadFromStream(request.DataStream, operationContext: uploadContext);
            var successfulPost = uploadContext.RequestResults.All(uploadRequest => uploadRequest.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
        }

        public async Task<CloudPlatformResponse> PostBlobAsync(CloudPlatformRequest request)
        {
            var uploadContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var blobContainer = BlobClient.GetContainerReference(request.Source);
            var blob = blobContainer.GetBlockBlobReference(request.Key);
            await blob.UploadFromStreamAsync(request.DataStream, null, null, uploadContext);
            var successfulPost = uploadContext.RequestResults.All(uploadRequest => uploadRequest.HttpStatusCode == 201);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
        }

        public Task<CloudPlatformResponse> PostBlobsAsync(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse ListBlobs(CloudPlatformRequest request)
        {
            var requestContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var client = storageAccount.CreateCloudBlobClient();
            var blobContainer = client.GetContainerReference(request.Source);
            var blobs = blobContainer.ListBlobs(operationContext: requestContext).Select(blob => (CloudBlockBlob)blob).ToList();
            response.Data = blobs;
            var successfulPost = requestContext.RequestResults.All(r => r.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            response.TimeStamp = requestContext.EndTime;
            return response;
        }

        public CloudPlatformResponse DeleteBlobs(CloudPlatformRequest request)
        {
            var response = new CloudPlatformResponse();
            var blobs = ListBlobs(request).Data as List<CloudBlockBlob> ?? new List<CloudBlockBlob>();
            Parallel.ForEach(blobs, blob => blob.Delete());
            response.HttpStatusCode = HttpStatusCode.OK;
            return response;
        }

        public CloudPlatformResponse GetFunctionName(string inputContainerName)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse GetInputOutputTriggers(string functionName)
        {
            throw new NotImplementedException();
        }
    }
}

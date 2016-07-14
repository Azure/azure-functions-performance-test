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

        private CloudQueueClient QueueClient
        {
            get { return storageAccount.CreateCloudQueueClient(); }
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
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var message = request.Data[Constants.Message] as string;
            var queue = QueueClient.GetQueueReference(request.Source);
            queue.AddMessage(new CloudQueueMessage(message), operationContext: operationContext);
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
        }

        public CloudPlatformResponse PostMessages(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var messages = request.Data[Constants.Message] as IEnumerable<string>;
            var queue = QueueClient.GetQueueReference(request.Source);
            foreach (var message in messages)
            {
                queue.AddMessage(new CloudQueueMessage(message), operationContext: operationContext);
            }
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 201);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
        }

        public CloudPlatformResponse GetMessage(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var queue = QueueClient.GetQueueReference(request.Source);
            var cloudMessage = queue.GetMessage(operationContext: operationContext);
            var messageString = cloudMessage.AsString;
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            response.Data = messageString;
            return response;
        }

        public CloudPlatformResponse GetMessages(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var messages = new List<CloudQueueMessage>();
            var queue = QueueClient.GetQueueReference(request.Source);
            queue.FetchAttributes();
            var queueLength = queue.ApproximateMessageCount;
            do
            {
                messages.AddRange(queue.GetMessages(Constants.MaxDequeueAmount));
            } while (messages.Count < queueLength);

            var messagesString = messages.Select(message => message.AsString);
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            response.Data = messagesString;
            return response;
        }

        public CloudPlatformResponse DeleteMessages(CloudPlatformRequest request)
        {
            var operationContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var client = storageAccount.CreateCloudQueueClient();
            var queue = client.GetQueueReference(request.Source);
            queue.FetchAttributes();
            var queueLength = queue.ApproximateMessageCount;
            do
            {
                var messages = queue.GetMessages(Constants.MaxDequeueAmount);
                foreach (var cloudQueueMessage in messages)
                {
                    queue.DeleteMessage(cloudQueueMessage, null, operationContext);
                }
                queue.FetchAttributes();
                queueLength = queue.ApproximateMessageCount;
            } while (queueLength > 0);
            var successfulPost = operationContext.RequestResults.All(cxt => cxt.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
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
            OperationContext uploadContext = new OperationContext();
            var response = new CloudPlatformResponse();
            var client = storageAccount.CreateCloudBlobClient();
            var blobContainer = client.GetContainerReference(request.Source);
            var blob = blobContainer.GetBlockBlobReference(request.Key);
            await blob.UploadFromStreamAsync(request.DataStream, null, null, uploadContext);
            var successfulPost = uploadContext.RequestResults.All(uploadRequest => uploadRequest.HttpStatusCode == 200);
            response.HttpStatusCode = successfulPost ? HttpStatusCode.OK : HttpStatusCode.Conflict;
            return response;
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

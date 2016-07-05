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
using Microsoft.WindowsAzure.Storage.Table;

namespace ServerlessBenchmark.ServerlessPlatformControllers.Azure
{
    public class AzureController:ICloudPlatformController
    {
        private readonly CloudStorageAccount storageAccount;
        public AzureController()
        {
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
            }
            else
            {
                throw new Exception("AzureStorageConnectionString does not exist in app config");
            }
        }

        public CloudPlatformResponse PostMessage(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse PostMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse GetMessage(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse GetMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse DeleteMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
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

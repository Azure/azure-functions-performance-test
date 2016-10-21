using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ServerlessBenchmark.ServerlessPlatformControllers.AWS
{
    public class AwsController: ICloudPlatformController
    {
        public Platform PlatformName => Platform.Amazon;
        public ILogger Logger { get; set;  } = new ConsoleLogger();

        protected AmazonSimpleNotificationServiceClient SnsClient
        {
            get
            {
                return new AmazonSimpleNotificationServiceClient();
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

        public async Task<CloudPlatformResponse> PostMessagesAsync(CloudPlatformRequest request)
        {
            IEnumerable<PublishResponse> completedPublishJobs;
            var topic = request.Data[Constants.Topic] as string;
            var messages = request.Data[Constants.Message] as IEnumerable<string>;
            var pendingPublishJobs = messages.Select(message => PublishMessage(topic, message, request.RetryAttempts)).ToList();
            var completedPublishJobsTasks = Task.WhenAll(pendingPublishJobs);
            var timeout = request.TimeoutMilliseconds ?? (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
            var timeoutTask = Task.Delay(timeout);

            var aggregateTasks = new List<Task>()
            {
                {completedPublishJobsTasks},
                {timeoutTask}
            };

            var doneTask = await Task.WhenAny(aggregateTasks);
            if (doneTask.Id == timeoutTask.Id)
            {
                this.Logger.LogException("--ERROR-- Reached timeout {0}ms", timeout);
                throw new Exception();
            }
            else
            {
                //the only other task that we were waiting for were the publish jobs
                completedPublishJobs = ((Task<PublishResponse[]>) doneTask).Result;
            }

            var areAnyFailed = completedPublishJobs.Any(publishJob => publishJob.HttpStatusCode != HttpStatusCode.OK);
            var requestResponse = new CloudPlatformResponse()
            {
                HttpStatusCode = areAnyFailed ? HttpStatusCode.Conflict : HttpStatusCode.OK,
                TimeStamp = DateTime.Now
            };
            return requestResponse;
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
            var sqsClient = new AmazonSQSClient();
            var cpResponse = new CloudPlatformResponse();
            try
            {
                var queueUrl = sqsClient.GetQueueUrl(request.Source).QueueUrl;
                var response = sqsClient.PurgeQueue(queueUrl);
                cpResponse.HttpStatusCode = response.HttpStatusCode;
            }
            catch (PurgeQueueInProgressException)
            {
                //retry
                Thread.Sleep(TimeSpan.FromSeconds(60));
                cpResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
            }
            return cpResponse;
        }

        public async Task<CloudPlatformResponse> EnqueueMessagesAsync(CloudPlatformRequest request)
        {
            var cResponse = new CloudPlatformResponse();
            try
            {
                SendMessageBatchResponse response;
                var messages = (IEnumerable<string>) request.Data[ServerlessBenchmark.Constants.Message];
                var batchMessageEntry =
                    messages.Select(message => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString("N"), message))
                        .ToList();
                using (var client = new AmazonSQSClient())
                {
                    response = await client.SendMessageBatchAsync(client.GetQueueUrl(request.Source).QueueUrl, batchMessageEntry);
                }
                if (response.Failed.Any())
                {
                    var groupedFailures = response.Failed.GroupBy(failure => failure.Message);
                    foreach (var group in groupedFailures)
                    {
                        cResponse.ErrorDetails.Add(group.Key, group.Count());
                    }
                    cResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
                }
                else
                {
                    cResponse.HttpStatusCode = HttpStatusCode.OK;                    
                }
            }
            catch (InvalidCastException)
            {
                this.Logger.LogWarning("Data needs to be IEnumberable of strings");
            }
            catch (Exception ex)
            {
                this.Logger.LogException(ex);
            }
            return cResponse;
        }

        public async Task<CloudPlatformResponse> DequeueMessagesAsync(CloudPlatformRequest request)
        {
            var cResponse = new CloudPlatformResponse();
            try
            {
                ReceiveMessageResponse response;
                using (var client = new AmazonSQSClient())
                {
                    var queueUrl = client.GetQueueUrl(request.Source).QueueUrl;
                    response = await client.ReceiveMessageAsync(queueUrl);

                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        cResponse.Data = response.Messages;
                        foreach (var message in response.Messages)
                        {
                            await client.DeleteMessageAsync(new DeleteMessageRequest(queueUrl, message.ReceiptHandle));
                        }
                    }
                    else
                    {
                        cResponse.ErrorDetails.Add("unknown", 1);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogException(ex);
            }
            return cResponse;
        }

        public async Task<CloudPlatformResponse> GetOutputItemsCount(CloudPlatformRequest request)
        {
            var cResponse = new CloudPlatformResponse();
            try
            {
                ReceiveMessageResponse response;
                using (var client = new AmazonSQSClient())
                {
                    var queueUrl = client.GetQueueUrl(request.Source).QueueUrl;
                    var length = await client.GetQueueAttributesAsync(queueUrl, new List<string> {"ApproximateNumberOfMessages"});
                    response = await client.ReceiveMessageAsync(queueUrl);

                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        cResponse.Data = length.ApproximateNumberOfMessages;
                    }
                    else
                    {
                        cResponse.Data = 0;
                        cResponse.ErrorDetails.Add("unknown", 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return cResponse;
        }

        public CloudPlatformResponse PostBlob(CloudPlatformRequest request)
        {
            AmazonWebServiceResponse response;
            if (request == null)
            {
                throw new ArgumentException("Request is null");
            }
            using (var client = new AmazonS3Client())
            {
                response = client.PutObject(new PutObjectRequest()
                {
                    BucketName = request.Source,
                    InputStream = request.DataStream,
                    AutoCloseStream = false,
                    Key = request.Key
                });
                var insertionTime = DateTime.UtcNow;
                if (!response.ResponseMetadata.Metadata.ContainsKey(ServerlessBenchmark.Constants.InsertionTime))
                {
                    response.ResponseMetadata.Metadata.Add(ServerlessBenchmark.Constants.InsertionTime, insertionTime.ToString("o"));
                }
            }
            return AwsCloudPlatformResponse.PopulateFrom(response);
        }

        public async Task<CloudPlatformResponse> PostBlobAsync(CloudPlatformRequest request)
        {
            AmazonWebServiceResponse response;
            if (request == null)
            {
                throw new ArgumentException("Request is null");
            }
            using (var client = new AmazonS3Client())
            {
                response = await client.PutObjectAsync(new PutObjectRequest()
                {
                    BucketName = request.Source,
                    InputStream = request.DataStream,
                    AutoCloseStream = false,
                    Key = request.Key
                });
                var insertionTime = DateTime.UtcNow;
                if (!response.ResponseMetadata.Metadata.ContainsKey(ServerlessBenchmark.Constants.InsertionTime))
                {
                    response.ResponseMetadata.Metadata.Add(ServerlessBenchmark.Constants.InsertionTime, insertionTime.ToString("o"));
                }
            }
            return AwsCloudPlatformResponse.PopulateFrom(response);
        }

        public Task<CloudPlatformResponse> PostBlobsAsync(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public CloudPlatformResponse ListBlobs(CloudPlatformRequest request)
        {
            var response = new CloudPlatformResponse();
            var blobs = new List<S3Object>();
            var listBlobRequest = new ListObjectsRequest()
            {
                BucketName = request.Source
            };
            using (var client = new AmazonS3Client())
            {
                ListObjectsResponse listResponse;
                do
                {
                    listResponse = client.ListObjects(listBlobRequest);
                    blobs.AddRange(listResponse.S3Objects);
                    listBlobRequest.Marker = listResponse.NextMarker;
                } while (listResponse.IsTruncated);   
            }
            response.Data = blobs;
            return response;
        }

        public CloudPlatformResponse DeleteBlobs(CloudPlatformRequest request)
        {
            var blobs = (IEnumerable<S3Object>)ListBlobs(request).Data;
            AmazonWebServiceResponse response = null;
            const int deleteLimit = 1000;
            if (blobs.Any())
            {
                using (var client = new AmazonS3Client(new AmazonS3Config()))

                {
                    try
                    {
                        var keys = blobs.Select(blob => new KeyVersion { Key = blob.Key }).ToList();
                        int num = keys.Count;
                        do
                        {
                            this.Logger.LogInfo("Deleting Blobs - Remaining:       {0}", num);
                            num -= (num < deleteLimit ? num : deleteLimit);
                            var takenKeys = keys.Take(deleteLimit).ToList();
                            response = client.DeleteObjects(new DeleteObjectsRequest()
                            {
                                BucketName = request.Source,
                                Objects = takenKeys
                            });
                            keys = keys.Except(takenKeys).ToList();
                        } while (num > 0);
                    }
                    catch (DeleteObjectsException e)
                    {
                        DeleteObjectsResponse errorResponse = e.Response;
                        foreach (DeleteError deleteError in errorResponse.DeleteErrors)
                        {
                            this.Logger.LogInfo("Error deleting item " + deleteError.Key);
                            this.Logger.LogInfo(" Code - " + deleteError.Code);
                            this.Logger.LogInfo(" Message - " + deleteError.Message);
                        }
                    }
                }
            }
            return AwsCloudPlatformResponse.PopulateFrom(response);
        }

        private async Task<PublishResponse> PublishMessage(string topic, string message, int retries = 3)
        {
            PublishResponse response = null;
            bool isSuccessPublish = false;
            do
            {
                try
                {
                    var findTopicRequest = SnsClient.FindTopic(topic);
                    if (findTopicRequest == null)
                    {
                        throw new Exception(String.Format("Topic {0} NotFound", topic));
                    }
                    response = await SnsClient.PublishAsync(findTopicRequest.TopicArn, message);
                    isSuccessPublish = true;
                }
                catch (Exception e)
                {
                    if (retries > 0)
                    {
                        this.Logger.LogWarning("Encountered error while publishing message to SNS topic: {0}", topic);
                        this.Logger.LogException(e);
                        this.Logger.LogInfo("Retrying...");
                        retries -= 1;
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (retries > 0 && !isSuccessPublish);
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

        public static class Constants
        {
            public const string Topic = "Topic";
            public const string Message = "Message";
            public const string Queue = "QueueUrl";
        }
    }
}

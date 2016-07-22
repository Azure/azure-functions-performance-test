using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
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
        public CloudPlatformResponse PostMessage(CloudPlatformRequest request)
        {
            var requiredParams = new string[] { Constants.Topic, Constants.Message};
            var missingParams = requiredParams.Where(param => !request.Data.ContainsKey(param)).ToList();
            if (missingParams.Any())
            {
                throw new ArgumentException(String.Format("Missing args: "), String.Join(" ", missingParams.ToArray()));
            }

            var topic = request.Data[Constants.Topic] as string;
            var message = request.Data[Constants.Message] as string;
            var response = SNS_PublishMessage(topic, message);
            var requestResponse = new CloudPlatformResponse()
            {
                HttpStatusCode = response.HttpStatusCode,
                TimeStamp = DateTime.Now
            };
            int temp;
            Int32.TryParse(response.MessageId, out temp);
            requestResponse.ResponseId = temp;
            return requestResponse;
        }

        public CloudPlatformResponse PostMessages(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<CloudPlatformResponse> PostMessagesAsync(CloudPlatformRequest request)
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
            var requiredParams = new string[]{Constants.Queue};
            var missingParams = requiredParams.Where(param => !request.Data.ContainsKey(param)).ToList();
            if (missingParams.Any())
            {
                throw new ArgumentException(String.Format("Missing args: "), String.Join(" ", missingParams.ToArray()));
            }
            var sqsClient = new AmazonSQSClient();
            var cpResponse = new CloudPlatformResponse();
            while (true)
            {
                try
                {
                    var response = sqsClient.PurgeQueue(request.Data[Constants.Queue] as string);
                    cpResponse.HttpStatusCode = response.HttpStatusCode;
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    break;
                }
                catch (PurgeQueueInProgressException)
                {
                    //retry
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    cpResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
                }
            }
            return cpResponse;
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

        public Task<CloudPlatformResponse> PostBlobAsync(CloudPlatformRequest request)
        {
            throw new NotImplementedException();
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
                            Console.WriteLine("Deleting Blobs - Remaining:       {0}", num);
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
                            Console.WriteLine("Error deleting item " + deleteError.Key);
                            Console.WriteLine(" Code - " + deleteError.Code);
                            Console.WriteLine(" Message - " + deleteError.Message);
                        }
                    }
                }
            }
            return AwsCloudPlatformResponse.PopulateFrom(response);
        }

        public PublishResponse SNS_PublishMessage(string topic, string message)
        {
            try
            {
                var snsClient = new AmazonSimpleNotificationServiceClient();
                var response = snsClient.Publish(topic, message);
                return response;
            }
            catch (InternalErrorException e)
            {
                Console.WriteLine("AWS let us down!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine(e);
            }
            return null;
        }

        public PerfTestResult PutPerfInfo(PerfTestResult perfTestResult, string functionInputContainer, string functionDestinationContainer)
        {
            using (var cwClient = new AmazonCloudWatchLogsClient())
            {
                var logStreams = new List<LogStream>();
                DescribeLogStreamsResponse lResponse;
                string nextToken = null;
                do
                {
                    lResponse =
                        cwClient.DescribeLogStreams(new DescribeLogStreamsRequest("/aws/lambda/ImageResizerV2")
                        {
                            NextToken = nextToken
                        });
                    logStreams.AddRange(lResponse.LogStreams);
                } while (!string.IsNullOrEmpty(nextToken = lResponse.NextToken));
                var logs =
                    logStreams.Select(
                        s =>
                            cwClient.GetLogEvents(new GetLogEventsRequest("/aws/lambda/ImageResizerV2",
                                s.LogStreamName)));
            }
            return perfTestResult;
        }

        public PerfTestResult PutPerfInfoAsync(PerfTestResult perfTestResult, string functionInputContainer,
            string functionDestinationContainer)
        {
            return perfTestResult;
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

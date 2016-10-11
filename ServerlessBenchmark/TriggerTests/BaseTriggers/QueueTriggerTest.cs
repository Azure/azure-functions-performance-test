using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessResultManager;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class QueueTriggerTest:StorageTriggerTest
    {
        private int _outPutQueueSize;
        protected string SourceQueue { get; set; }
        protected string TargetQueue { get; set; }

        protected QueueTriggerTest(string functionName, int eps, string[] messages, string sourceQueue, string targetQueue):base(functionName, eps, messages)
        {
            SourceQueue = sourceQueue;
            TargetQueue = targetQueue;
        }

        protected override List<CloudPlatformResponse> CleanUpStorageResources()
        {
            List<CloudPlatformResponse> cloudPlatformResponses = null;
            try
            {
                cloudPlatformResponses = new List<CloudPlatformResponse>
                {
                    {CloudPlatformController.DeleteMessages(new CloudPlatformRequest() {Source = SourceQueue})},
                    {CloudPlatformController.DeleteMessages(new CloudPlatformRequest() {Source = TargetQueue})}
                };
            }
            catch (Exception e)
            {
                var webException = e.InnerException as WebException;
                if (webException != null)
                {
                    if (webException.Status == WebExceptionStatus.ProtocolError && webException.Message.Contains("400"))
                    {
                        throw new Exception(String.Format("Error: 404 when deleting message from queue. Check that queues exist: {0} {1}", SourceQueue, TargetQueue));
                    }
                }
                throw;
            }
            return cloudPlatformResponses;
        }

        protected override string StorageType
        {
            get { return "Queue"; }
        }

        protected async override Task<bool> VerifyTargetDestinationStorageCount(int expectedCount)
        {
            return await VerifyQueueMessagesExistInTargetQueue(expectedCount);
        }

        protected override async Task UploadItems(IEnumerable<string> items)
        {
            await UploadMessagesAsync(items);
        }

        private async Task UploadMessagesAsync(IEnumerable<string> messages)
        {
            await CloudPlatformController.EnqueueMessagesAsync(new CloudPlatformRequest()
            {
                Key = Guid.NewGuid().ToString(),
                Source = SourceQueue,
                Data = new Dictionary<string, object>()
                    {
                        {Constants.Message, messages}
                    }
            });

            var currentFinished = await CloudPlatformController.GetOutputItemsCount(new CloudPlatformRequest()
            {
                Key = Guid.NewGuid().ToString(),
                Source = TargetQueue
            });

            var progressResult = new TestResult
            {
                Timestamp = DateTime.UtcNow,
                CallCount = (int)currentFinished.Data - _outPutQueueSize,
                FailedCount = 0,
                SuccessCount = (int)currentFinished.Data - _outPutQueueSize,
                TimeoutCount = 0,
                AverageLatency = 0
            };

            _outPutQueueSize = (int)currentFinished.Data;

            this.TestRepository.AddTestResult(this.TestWithResults, progressResult);
        }

        protected override Task TestCoolDown()
        {
            return Task.FromResult(true);
        }

        private async Task<bool> VerifyQueueMessagesExistInTargetQueue(int expected)
        {
            IEnumerable<object> messages;
            var lastCountSeen = -1;
            int count = 0;
            DateTime lastTimeCountChanged = new DateTime();
            var timeout = TimeSpan.FromSeconds(45);
            var startTime = DateTime.UtcNow;
            do
            {
                var taskMesagesResponse = await CloudPlatformController.DequeueMessagesAsync(new CloudPlatformRequest()
                {
                    Source = TargetQueue
                });
                messages = (IEnumerable<object>) taskMesagesResponse.Data;
                count += messages == null ? 0 : messages.Count();
                Console.WriteLine("Destination Messages - Number Of Messages:     {0}", count);
                Thread.Sleep(1 * 1000);
                if (count != lastCountSeen)
                {
                    lastCountSeen = count;
                    lastTimeCountChanged = DateTime.UtcNow;
                }

                if ((startTime - lastTimeCountChanged) > timeout)
                {
                    Console.WriteLine("Waiting for destination queue to reach expected count timed out: {0}/{1}", count, ExpectedExecutionCount);
                    break;
                }
            } while (count < expected);
            return true;
        }
    }
}

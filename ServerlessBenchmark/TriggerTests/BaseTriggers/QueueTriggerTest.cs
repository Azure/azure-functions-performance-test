using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class QueueTriggerTest:StorageTriggerTest
    {
        protected abstract bool SetUp();
        private string SourceQueue { get; set; }
        private string TargetQueue { get; set; }

        protected QueueTriggerTest(string functionName, string[] messages, string sourceQueue, string targetQueue):base(functionName, messages)
        {
            SourceQueue = sourceQueue;
            TargetQueue = targetQueue;
        }

        protected override List<CloudPlatformResponse> CleanUpStorageResources()
        {
            var cloudPlatformResponses = new List<CloudPlatformResponse>
                {
                    {CloudPlatformController.DeleteMessages(new CloudPlatformRequest() {Source = SourceQueue})},
                    {CloudPlatformController.DeleteMessages(new CloudPlatformRequest() {Source = TargetQueue})}
                };
            if (SetUp())
            {
                return cloudPlatformResponses;
            }
            else
            {
                throw new Exception("Test Clean up failed");
            }
        }

        protected override string StorageType
        {
            get { return "Queue"; }
        }

        protected override bool VerifyTargetDestinationStorageCount(int expectedCount)
        {
            return VerifyQueueMessagesExistInTargetQueue(expectedCount);
        }

        protected override async Task UploadItems(IEnumerable<string> items)
        {
            await UploadMessagesAsync(items);
        }

        private void UploadMessages(IEnumerable<string> messages)
        {
            CloudPlatformController.PostMessages(new CloudPlatformRequest()
            {
                Key = Guid.NewGuid().ToString(),
                Source = SourceQueue,
                Data = new Dictionary<string, object>()
                    {
                        {Constants.Message, messages}
                    }
            });
        }

        private async Task UploadMessagesAsync(IEnumerable<string> messages)
        {
            await CloudPlatformController.PostMessagesAsync(new CloudPlatformRequest()
            {
                Key = Guid.NewGuid().ToString(),
                Source = SourceQueue,
                Data = new Dictionary<string, object>()
                    {
                        {Constants.Message, messages}
                    }
            });
        }

        private bool VerifyQueueMessagesExistInTargetQueue(int expected)
        {
            IEnumerable<object> messages;
            var lastCountSeen = -1;
            var lastTimeCountChanged = DateTime.MinValue;
            do
            {
                messages = (IEnumerable<object>)CloudPlatformController.GetMessages(new CloudPlatformRequest()
                {
                    Source = TargetQueue
                }).Data;
                Console.WriteLine("Destination Messages - Number Of Messages:     {0}", messages.Count());
                Thread.Sleep(1 * 1000);
                var count = messages.Count();
                if (count != lastCountSeen)
                {
                    lastCountSeen = count;
                    lastTimeCountChanged = DateTime.UtcNow;
                }
            } while (messages.Count() < expected && DateTime.UtcNow < lastTimeCountChanged.AddMinutes(-4));
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests
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

        protected override void UploadItems(IEnumerable<string> items)
        {
            UploadMessages(items);
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

        private bool VerifyQueueMessagesExistInTargetQueue(int expected)
        {
            IEnumerable<object> messages;
            do
            {
                messages = (IEnumerable<object>)CloudPlatformController.GetMessages(new CloudPlatformRequest()
                {
                    Source = TargetQueue
                }).Data;
                Console.WriteLine("Destination Messages - Number Of Messages:     {0}", messages.Count());
                Thread.Sleep(1 * 1000);
            } while (messages.Count() < expected);
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonSnsToSqs : AmazonSqsTriggerTest
    {
        private string _sourceTopic;
        public AmazonSnsToSqs(string functionName, IEnumerable<string> messages, string sourceTopic, string destinationQueue) : base(functionName, messages, sourceTopic, destinationQueue)
        {
            _sourceTopic = sourceTopic;
        }

        protected override string StorageType
        {
            get { return "SnsToQueue"; }
        }

        protected override List<CloudPlatformResponse> CleanUpStorageResources()
        {
            var responses = new List<CloudPlatformResponse>();
            List<CloudPlatformResponse> cloudPlatformResponses = null;
            try
            {
                cloudPlatformResponses = new List<CloudPlatformResponse>
                {
                    {CloudPlatformController.DeleteMessages(new CloudPlatformRequest() {Source = TargetQueue})}
                };
                responses.AddRange(cloudPlatformResponses);
            }
            catch (Exception e)
            {
                this.Logger.LogException(e);
                throw;
            }
            return responses;
        }

        protected async override Task UploadItems(IEnumerable<string> items)
        {
            //instead of sending messages to queue send to SNS topic
            await CloudPlatformController.PostMessagesAsync(new CloudPlatformRequest()
            {
                Data = new Dictionary<string, object>()
                {
                    {Constants.Message, items},
                    {Constants.Topic, _sourceTopic}
                },
                Retry = true,
                RetryAttempts = 3,
                TimeoutMilliseconds = (int?) TimeSpan.FromSeconds(45).TotalMilliseconds
            });
        }
    }
}

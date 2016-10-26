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
        private int _itemsPut;
        private int _itemsPutInGeneral;
        private int _lastIterationFinished;
        private int _tickTimeInMiliseconds = 1000;
        protected string SourceQueue { get; set; }
        protected string TargetQueue { get; set; }

        protected QueueTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, string[] messages, string sourceQueue, string targetQueue):base(functionName, eps, warmUpTimeInMinutes, messages)
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

        protected override bool TestSetupWithRetry()
        {
            _outPutQueueSize = 0;
            _itemsPut = 0;
            _itemsPutInGeneral = 0;
            _lastIterationFinished = 0;
            return base.TestSetupWithRetry();
        }

        protected override void SaveCurrentProgessToDb()
        {
            var progressResult = new TestResult
            {
                Timestamp = DateTime.UtcNow,
                CallCount = _itemsPut,
                FailedCount = 0,
                SuccessCount = _lastIterationFinished,
                TimeoutCount = 0,
                AverageLatency = 0
            };

            
            this.TestRepository.AddTestResult(this.TestWithResults, progressResult);
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
            var currentFinished = await GetCurrentOutputQueueSize();
            _lastIterationFinished = (int)currentFinished.Data - _outPutQueueSize;
            _outPutQueueSize = (int)currentFinished.Data;
            await UploadMessagesAsync(items);
            _itemsPutInGeneral += items.Count();
            _itemsPut = items?.Count() ?? 0;
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
        }

        protected override async Task TestCoolDown()
        {
            // wait until destination queue won't be growing for 1 minutes
            var lastSize = 0;
            DateTime lastNewSize = new DateTime();
            string testProgressString;
            while (true)
            {
                try
                {
                    _itemsPut = 0;

                    if (!this.DuringWarmUp)
                    {
                        this.SaveCurrentProgessToDb();
                    }

                    var currentOutPutQueueSize = await GetCurrentOutputQueueSize();
                    _lastIterationFinished = (int)currentOutPutQueueSize.Data - _outPutQueueSize;
                    _outPutQueueSize = (int)currentOutPutQueueSize.Data;
                    testProgressString = PrintTestProgress();
                    testProgressString = $"OutStanding:    {_itemsPutInGeneral - _outPutQueueSize} (finish: {_outPutQueueSize}/{_itemsPutInGeneral})   {testProgressString}";

                    if (_itemsPutInGeneral <= (int)currentOutPutQueueSize.Data)
                    {
                        Logger.LogInfo(testProgressString);
                        Logger.LogInfo("Finished Outstanding Requests");
                        break;
                    }

                    if (_lastIterationFinished != 0)
                    {
                        lastNewSize = DateTime.Now;
                    }
                    else
                    {
                        var secondsSinceLastNewSize = (DateTime.Now - lastNewSize).TotalSeconds;
                        var secondsLeft = TimeSpan.FromMilliseconds(Constants.LoadCoolDownTimeout).TotalSeconds - secondsSinceLastNewSize;
                        Logger.LogInfo("No new items on destination queue for {0} seconds. Waiting another {1}s to finish", secondsSinceLastNewSize, secondsLeft);

                        if (secondsLeft < 0)
                        {
                            break;
                        }
                    }

                    Logger.LogInfo(testProgressString);
                    await Task.Delay(_tickTimeInMiliseconds);
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }
            }

            // clear data after run
            _outPutQueueSize = 0;
            _itemsPut = 0;
            _itemsPutInGeneral = 0;
            _lastIterationFinished = 0;
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
                var retrieveCount = messages.Count();
                count += messages == null ? 0 : retrieveCount;
                this.Logger.LogInfo("Destination Messages - Number Of Messages:     {0}", count);

                if(retrieveCount < Constants.MaxDequeueAmount)
                    Thread.Sleep(1 * 1000);
                
                if (count != lastCountSeen)
                {
                    lastCountSeen = count;
                    lastTimeCountChanged = DateTime.UtcNow;
                }

                if ((startTime - lastTimeCountChanged) > timeout)
                {
                    Logger.LogInfo("Waiting for destination queue to reach expected count timed out: {0}/{1}", count, ExpectedExecutionCount);
                    break;
                }
            } while (count < expected);
            return true;
        }

        private async Task<CloudPlatformResponse> GetCurrentOutputQueueSize()
        {
            return await CloudPlatformController.GetOutputItemsCount(new CloudPlatformRequest()
            {
                Key = Guid.NewGuid().ToString(),
                Source = TargetQueue
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniCommandLineHelper;
using ServerlessBenchmark;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages
{
    public class Program:CmdHelper
    {
        public new static void Main(string[] args)
        {
            var p = new Program();
            ((CmdHelper) p).Main(args);
        }

        #region LambdaTests
        [Command]
        public void S3Test(string functionName, string blobPath, string srcBucket, string targetBucket, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var blobs = Directory.GetFiles(blobPath);
            var test = new AmazonS3TriggerTest(functionName, blobs, srcBucket, targetBucket);
            StorageTriggerTest(test, blobs, loadProfile, eps, repeat, durationMinutes);
        }

        [Command]
        public void SqsTest(string functionName, string messages, string srcQueue, string targetQueue,
            string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var queueMessages = File.ReadAllLines(messages);
            var test = new AmazonSqsTriggerTest(functionName, queueMessages, srcQueue, targetQueue);
            StorageTriggerTest(test, queueMessages, loadProfile, eps, repeat, durationMinutes);
        }

        [Command]
        public void ApiGatewayTest(string functionName, string urlsFile, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var urls = File.ReadAllLines(urlsFile);
            var test = new AmazonApiGatewayTriggerTest(functionName, urls);
            HttpTriggerTest(test, urls, loadProfile, eps, repeat, durationMinutes);
        }

        [Command]
        public void SnsToSqsTest(string functionName, string messages, string srcTopic, string targetQueue,
            string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var queueMessages = File.ReadAllLines(messages);
            var test = new AmazonSnsToSqs(functionName, queueMessages, srcTopic, targetQueue);
            StorageTriggerTest(test, queueMessages, loadProfile, eps, repeat, durationMinutes);
        }
        #endregion

        #region AzureFunctionTest

        [Command]
        public void BlobTest(string functionName, string blobPath, string srcBlobContainer,
            string targetBlobContainer, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            AzureStorageTest(TriggerTypes.Blob, functionName, blobPath, srcBlobContainer, targetBlobContainer, loadProfile, eps, repeat, durationMinutes);
        }

        [Command]
        public void QueueTest(string functionName, string queueItems, string srcQueue,
            string targetQueue, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            AzureStorageTest(TriggerTypes.Queue, functionName, queueItems, srcQueue, targetQueue, loadProfile, eps, repeat, durationMinutes);
        }

        [Command]
        public void AzureHttpTest(string functionName, string urlsFile, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var urls = File.ReadAllLines(urlsFile);
            var test = new AzureHttpTriggerTest(functionName, urls);
            HttpTriggerTest(test, urls, loadProfile, eps, repeat, durationMinutes);
        }

        private void AzureStorageTest(TriggerTypes triggerType, string functionName, string items, string source, string target, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            FunctionTest test;
            switch (triggerType)
            {
                case TriggerTypes.Blob:
                    var blobs = Directory.GetFiles(items);
                    test = new AzureBlobTriggerTest(functionName, blobs, source, target);
                    StorageTriggerTest(test, blobs, loadProfile, eps, repeat, durationMinutes);
                    break;
                case TriggerTypes.Queue:
                    var queueMessages = File.ReadAllLines(items);
                    test = new AzureQueueTriggerTest(functionName, queueMessages, source, target);
                    StorageTriggerTest(test, queueMessages, loadProfile, eps, repeat, durationMinutes);
                    break;
            }
        }
        #endregion

        private void HttpTriggerTest(FunctionTest functionTest, IEnumerable<string> urls, string loadProfile, int eps = 0, bool repeat = false,
            int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && repeat)
            {
                if (durationMinutes <= 0)
                {
                    throw new ArgumentException("No parameter to specify how long to repeat this load. Indicate how long in minutes to repeat load.", "durationMinutes");
                }
                profile = new LinearLoad(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
            }
            else if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && !repeat)
            {
                profile = new LinearLoad(urls.Count(), eps == 0 ? 1 : eps);
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }
            var perfResult = functionTest.RunAsync(profile).Result;

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(perfResult);
            Console.ForegroundColor = originalColor;
        }

        private void StorageTriggerTest(FunctionTest functionTest, IEnumerable<string> sourceItems, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;

            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && repeat)
            {
                if (durationMinutes <= 0)
                {
                    throw new ArgumentException("No parameter to specify how long to repeat this load. Indicate how long in minutes to repeat load.", "durationMinutes");
                }
                profile = new LinearLoad(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
            }
            else if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && !repeat)
            {
                profile = new LinearLoad(sourceItems.Count(), eps == 0 ? 1 : eps);
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }

            var perfResult = functionTest.RunAsync(profile).Result;

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(perfResult);
            Console.ForegroundColor = originalColor;
        }
    }
}

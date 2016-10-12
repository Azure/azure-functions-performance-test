using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using MiniCommandLineHelper;
using ServerlessBenchmark;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages
{
    public class Program : CmdHelper
    {
        public new static void Main(string[] args)
        {
            var p = new Program();
            ((CmdHelper) p).Main(args);
        }

        #region LambdaTests
        [Command]
        public void S3Test(string functionName, string blobPath, string srcBucket, string targetBucket, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            var blobs = Directory.GetFiles(blobPath);
            var test = new AmazonS3TriggerTest(functionName, eps, warmUpTimeInMinutes, blobs, srcBucket, targetBucket);
            StorageTriggerTest(test, blobs, loadProfile, repeat, durationMinutes);
        }

        [Command]
        public void SqsTest(string functionName, string messages, string srcQueue, string targetQueue,
            string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            var queueMessages = File.ReadAllLines(messages);
            var test = new AmazonSqsTriggerTest(functionName, eps, warmUpTimeInMinutes, queueMessages, srcQueue, targetQueue);
            StorageTriggerTest(test, queueMessages, loadProfile, repeat, durationMinutes);
        }

        [Command]
        public void ApiGatewayTest(string functionName, string urlsFile, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            var urls = File.ReadAllLines(urlsFile);
            var test = new AmazonApiGatewayTriggerTest(functionName, eps, warmUpTimeInMinutes, urls);
            HttpTriggerTest(test, urls, loadProfile, repeat, durationMinutes);
        }

        [Command]
        public void SnsToSqsTest(string functionName, string messages, string srcTopic, string targetQueue,
            string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            var queueMessages = File.ReadAllLines(messages);
            var test = new AmazonSnsToSqs(functionName, eps, warmUpTimeInMinutes, queueMessages, srcTopic, targetQueue);
            StorageTriggerTest(test, queueMessages, loadProfile, repeat, durationMinutes);
        }

        [Command]
        public void AnalyzeAwsTest(string functionName, DateTime startTime, DateTime endTime)
        {
            var resultsProvider = new AwsGenericPerformanceResultsProvider();
            var results = resultsProvider.GetPerfMetrics(functionName, startTime, endTime);
            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(results);
            Console.ForegroundColor = originalColor;
        }
        #endregion

        #region AzureFunctionTest

        [Command]
        public void BlobTest(string functionName, string blobPath, string srcBlobContainer,
            string targetBlobContainer, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            AzureStorageTest(TriggerTypes.Blob, functionName, blobPath, srcBlobContainer, targetBlobContainer, loadProfile, eps, repeat, durationMinutes, durationMinutes);
        }

        [Command]
        public void QueueTest(string functionName, string queueItems, string srcQueue,
            string targetQueue, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            AzureStorageTest(TriggerTypes.Queue, functionName, queueItems, srcQueue, targetQueue, loadProfile, eps, repeat, durationMinutes, warmUpTimeInMinutes);
        }

        [Command]
        public void AzureHttpTest(string functionName, string urlsFile, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            var urls = File.ReadAllLines(urlsFile);
            var test = new AzureHttpTriggerTest(functionName, eps, warmUpTimeInMinutes, urls);
            HttpTriggerTest(test, urls, loadProfile, repeat, durationMinutes);
        }

        private void AzureStorageTest(TriggerTypes triggerType, string functionName, string items, string source, string target, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            FunctionTest test;
            switch (triggerType)
            {
                case TriggerTypes.Blob:
                    var blobs = Directory.GetFiles(items);
                    test = new AzureBlobTriggerTest(functionName, eps, warmUpTimeInMinutes, blobs, source, target);
                    StorageTriggerTest(test, blobs, loadProfile, repeat, durationMinutes);
                    break;
                case TriggerTypes.Queue:
                    var queueMessages = File.ReadAllLines(items);
                    test = new AzureQueueTriggerTest(functionName, eps, warmUpTimeInMinutes, queueMessages, source, target);
                    StorageTriggerTest(test, queueMessages, loadProfile, repeat, durationMinutes);
                    break;
            }
        }

        [Command]
        public void AnalyzeAzureTest(string functionName, DateTime startTime, DateTime endTime)
        {
            var resultsProvider = new AzureGenericPerformanceResultsProvider();
            var results = resultsProvider.GetPerfMetrics(functionName, startTime, endTime);
            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(results);
            Console.ForegroundColor = originalColor;
        }
        #endregion

        [Command]
        public void PurgeFunctionsTable(string storageConnectionString = null)
        {
            var isPurged = FunctionLogs.PurgeAzureFunctionTableAsync(storageConnectionString).Result;
            Console.WriteLine($"Azure function log table purged:    {isPurged}");
        }

        private void HttpTriggerTest(FunctionTest functionTest, IEnumerable<string> urls, string loadProfile, bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            var eps = functionTest.Eps;

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
            else if (loadProfile.Equals("LinearRamp", StringComparison.CurrentCultureIgnoreCase))
            {
                profile = new LinearWithRampUp(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
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

        private void StorageTriggerTest(FunctionTest functionTest, IEnumerable<string> sourceItems, string loadProfile, bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            var eps = functionTest.Eps;

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
            else if (loadProfile.Equals("LinearRamp", StringComparison.CurrentCultureIgnoreCase))
            {
                profile = new LinearWithRampUp(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
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

using System;
using System.Collections.Generic;
using System.IO;
using MiniCommandLineHelper;
using Newtonsoft.Json;
using ServerlessBenchmark;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;
using ServerlessResultManager;

namespace SampleUsages
{
    public class Program : CmdHelper
    {
        public new static void Main(string[] args)
        {
            FunctionLogs._logger = new ConsoleLogger();
            var p = new Program();
            ((CmdHelper) p).Main(args);
        }

        [Command]
        public void RunScenario(string scenarioFilePath)
        {
            var testScenarios = new List<TestScenario>();
            using (StreamReader r = new StreamReader(scenarioFilePath))
            {
                string json = r.ReadToEnd();
                try
                {
                    testScenarios = JsonConvert.DeserializeObject<List<TestScenario>>(json);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse input file. {ex}");
                    return;
                }
            }

            Console.WriteLine("Start running scenarios.");
            var counter = 0;
            var now = DateTime.UtcNow;

            foreach (var testScenario in testScenarios)
            {
                Console.WriteLine($"Start running scenario for function {testScenario.FunctionName} {++counter}/{testScenarios.Count}.");
                var testFolder = string.Format(testScenario.FunctionName);
                Directory.CreateDirectory(testFolder);
                var logFilePath = $"{testFolder}/{now.ToString("yyyy-M-d-HH-mm")}-{testScenario.FunctionName}.log";

                using (var logger = new FileLogger(logFilePath))
                {
                    try
                    {
                        FunctionLogs._logger = logger;
                        testScenario.RunScenario(logger);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error while running {testScenario.FunctionName} check {logFilePath} for log details.");
                        Console.WriteLine($"Exception {e}");
                    }
                }

                Console.WriteLine($"Finished running scenario {counter}/{testScenarios.Count}.");
            }
        }

        #region LambdaTests
        [Command]
        public void S3Test(string functionName, string blobPath, string srcBucket, string targetBucket, LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Amazon,
                TriggerType.Blob,
                loadProfile,
                functionName,
                blobPath,
                inputObject: srcBucket,
                outputObject: targetBucket,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void SqsTest(string functionName, string messages, string srcQueue, string targetQueue,
            LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Amazon,
                TriggerType.AmazonSqsOnly,
                loadProfile,
                functionName,
                messages,
                inputObject: srcQueue,
                outputObject: targetQueue,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void ApiGatewayTest(string functionName, string urlsFile, LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Amazon,
                TriggerType.Http,
                loadProfile,
                functionName,
                urlsFile,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void SnsToSqsTest(string functionName, string messages, string srcTopic, string targetQueue,
            LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Amazon,
                TriggerType.Queue,
                loadProfile,
                functionName,
                messages,
                inputObject: srcTopic,
                outputObject: targetQueue,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
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
            string targetBlobContainer, LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Azure,
                TriggerType.Blob,
                loadProfile,
                functionName,
                blobPath,
                inputObject: srcBlobContainer,
                outputObject: targetBlobContainer,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void QueueTest(string functionName, string queueItems, string srcQueue,
            string targetQueue, LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Azure,
                TriggerType.Queue,
                loadProfile,
                functionName,
                queueItems,
                inputObject: srcQueue,
                outputObject: targetQueue,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void AzureHttpTest(string functionName, string urlsFile, LoadProfilesType loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0, int warmUpTimeInMinutes = 0)
        {
            RunScenarioWithParameters(
                Platform.Azure,
                TriggerType.Http,
                loadProfile,
                functionName,
                urlsFile,
                eps: eps,
                repeat: repeat,
                durationMinutes: durationMinutes,
                warmUpTimeInMinutes: warmUpTimeInMinutes);
        }

        [Command]
        public void AnalyzeAzureTest(string functionName, DateTime startTime, DateTime endTime, int testId = 0)
        {
            var resultsProvider = new AzureGenericPerformanceResultsProvider();
            TestRepository repo = null;
            if (testId != 0)
            {
                repo = new TestRepository();
                resultsProvider.DatabaseTest = repo.GetTest(testId, fetchResults: true);
            }

            var results = resultsProvider.GetPerfMetrics(functionName, startTime, endTime);

            if (testId != 0)
            {
                repo.UpdateTest(resultsProvider.DatabaseTest, saveResults: true);
            }

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

        private void RunScenarioWithParameters(
            Platform platform,
            TriggerType triggerType,
            LoadProfilesType loadProfile,
            string functionName,
            string inputPath,
            string inputObject = null,
            string outputObject = null,
            int eps = 0,
            bool repeat = true,
            int durationMinutes = 0,
            int warmUpTimeInMinutes = 0)
        {
            var scenario = new TestScenario
            {
                Platform = platform,
                TriggerType = triggerType,
                FunctionName = functionName,
                InputPath = inputPath,
                InputObject = inputObject,
                OutputObject = outputObject,
                LoadProfile = loadProfile,
                Eps = eps,
                Repeat = repeat,
                DurationInMinutes = durationMinutes,
                WarmUpTimeInMinutes = warmUpTimeInMinutes
            };

            scenario.RunScenario(new ConsoleLogger());
        }
    }
}

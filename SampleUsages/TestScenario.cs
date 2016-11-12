using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ServerlessBenchmark;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;
using System.Collections.Generic;
using ServerlessResultManager;

namespace SampleUsages
{
    class TestScenario
    {
        private string[] _input;

        public string FunctionName { get; set; }
        public Platform Platform { get; set; }
        public TriggerType TriggerType { get; set; }
        public LoadProfilesType LoadProfile { get; set; }
        public string InputPath { get; set; }
        public string[] Input { get { return _input; } set { _input = (value != null && value.Length == 0) ? null : value; } }
        public string InputObject { get; set; }
        public string OutputObject { get; set; }
        public int Eps { get; set; }
        public bool Repeat { get; set; }
        public int DurationInMinutes { get; set; }
        public int WarmUpTimeInMinutes { get; set; }
        public string AzureStorageConnectionStringConfigName { get; set; }
        private ILogger _logger { get; set; }

        internal void RunScenario(ILogger logger, ServerlessResultManager.TestScenario databaseTestScenario = null)
        {
            this._logger = logger;
            var scenarioType = Tuple.Create(this.Platform, this.TriggerType);
            FunctionTest test = null;
            TriggerTestLoadProfile profile;

            string[] inputItems;

            inputItems = this.Input;

            if (inputItems == null)
            {
                if (Directory.Exists(this.InputPath))
                {
                    inputItems = Directory.GetFiles(this.InputPath);
                }
                else
                {
                    inputItems = File.ReadAllLines(this.InputPath);
                }
            }

            if (scenarioType.Compare(Platform.Amazon, TriggerType.Blob))
            {
                AssertInput("FunctionName", "InputPath", "InputObject", "OutputObject");
                test = new AmazonS3TriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems, this.InputObject, this.OutputObject);
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.Http))
            {
                AssertInput("FunctionName", "InputPath");
                test = new AmazonApiGatewayTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems);
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.Queue))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                test = new AmazonSnsToSqs(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems, this.InputObject, this.OutputObject);
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.AmazonSqsOnly))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                test = new AmazonSqsTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems, this.InputObject, this.OutputObject);
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Blob))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                test = new AzureBlobTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems, this.InputObject, this.OutputObject, this.AzureStorageConnectionStringConfigName);
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Http))
            {
                AssertInput("InputPath", "FunctionName");
                test = new AzureHttpTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems);
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Queue))
            {
                AssertInput("FunctionName", "InputObject", "OutputObject");
                test = new AzureQueueTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, inputItems, this.InputObject, this.OutputObject, this.AzureStorageConnectionStringConfigName);
            }
            else
            {
                var ex = new ArgumentException(
                        $"Unknown combination of platform ({this.Platform}) and trigger type ({this.TriggerType}) available Platform values are ({string.Join(",", Enum.GetValues(typeof(Platform)))}) available TriggerType values are ({string.Join(",", Enum.GetValues(typeof(TriggerType)))})");
                this._logger.LogException(ex);
                throw ex;
            }

            Directory.CreateDirectory(this.FunctionName);
            profile = GetLoadProfile(inputItems.Count());
            test.Logger = _logger;

            var dbTest = new Test
            {
                Source = this.InputObject,
                Destination = this.OutputObject,
                TriggerType = this.TriggerType.ToString(),
                Platform = this.Platform.ToString(),
                TargetEps = this.Eps,
                ToolsVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            };

            dbTest.TestScenarioId = databaseTestScenario?.Id;
            test.TestWithResults = dbTest;
            RunScenario(test, profile);
        }

        private TriggerTestLoadProfile GetLoadProfile(int itemsCount)
        {
            TriggerTestLoadProfile profile;
            AssertInput("Eps");

            switch (this.LoadProfile)
            {
                case LoadProfilesType.Linear:
                    if (this.Repeat)
                    {
                        AssertInput("DurationInMinutes");
                        profile = new LinearLoad(TimeSpan.FromMinutes(this.DurationInMinutes),
                            this.Eps == 0 ? 1 : this.Eps);
                    }
                    else
                    {
                        profile = new LinearLoad(itemsCount, this.Eps == 0 ? 1 : this.Eps);
                    }
                    break;
                case LoadProfilesType.LinearRampUp:
                    profile = new LinearWithRampUp(TimeSpan.FromMinutes(this.DurationInMinutes),
                        this.Eps == 0 ? 1 : this.Eps);
                    break;
                default:
                    var ex = new ArgumentException(
                        $"Unknown load profile {this.LoadProfile} available values are ({string.Join(",", Enum.GetValues(typeof(LoadProfilesType)))})");
                    this._logger.LogException(ex);
                    throw ex;
            }

            return profile;
        }

        internal void RunScenario(FunctionTest functionTest, TriggerTestLoadProfile profile)
        {
            var perfResult = functionTest.RunAsync(profile).GetAwaiter().GetResult();
            _logger.LogInfo(perfResult.ToString());
        }

        private void AssertInput(params string[] properties)
        {
            var fault = false;
            var exceptions = new List<Exception>();

            foreach (var propertyName in properties)
            {
                var property = typeof(TestScenario).GetProperty(propertyName);
                if (property.GetValue(this) == null)
                {
                    var exception = new ArgumentException($"Required test parameter {property.Name} has not been specified.");
                    exceptions.Add(exception);
                    this._logger.LogException(exception);
                    fault = true;
                }
            }

            if (fault)
            {
                throw new AggregateException(exceptions);
            }
        }
    }

    static class CompareTuple
    {
        public static bool Compare<T1, T2>(this Tuple<T1, T2> value, T1 v1, T2 v2)
        {
            return value.Item1.Equals(v1) && value.Item2.Equals(v2);
        }
    }
}
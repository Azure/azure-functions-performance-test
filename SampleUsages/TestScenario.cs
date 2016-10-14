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

namespace SampleUsages
{
    class TestScenario
    {
        public string FunctionName { get; set; }
        public Platform Platform { get; set; }
        public TriggerType TriggerType { get; set; }
        public LoadProfilesType LoadProfile { get; set; }
        public string InputPath { get; set; }
        public string InputObject { get; set; }
        public string OutputObject { get; set; }
        public int Eps { get; set; }
        public bool Repeat { get; set; }
        public int DurationInMinutes { get; set; }
        public int WarmUpTimeInMinutes { get; set; }
        private ILogger _logger { get; set; }

        internal void RunScenario(ILogger logger)
        {
            this._logger = logger;
            var scenarioType = Tuple.Create(this.Platform, this.TriggerType);
            FunctionTest test = null;
            TriggerTestLoadProfile profile;
            var inputCount = 0;

            if (scenarioType.Compare(Platform.Amazon, TriggerType.Blob))
            {
                AssertInput("FunctionName", "InputPath", "InputObject", "OutputObject");
                var blobs = Directory.GetFiles(this.InputPath);
                test = new AmazonS3TriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, blobs, this.InputObject, this.OutputObject);
                inputCount = blobs.Count();
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.Http))
            {
                AssertInput("FunctionName", "InputPath");
                var urls = File.ReadAllLines(this.InputPath);
                test = new AmazonApiGatewayTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, urls);
                inputCount = urls.Count();
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.Queue))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                var queueMessages = File.ReadAllLines(this.InputPath);
                test = new AmazonSnsToSqs(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, queueMessages, this.InputObject, this.OutputObject);
                inputCount = queueMessages.Count();
            }
            else if (scenarioType.Compare(Platform.Amazon, TriggerType.AmazonSqsOnly))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                var queueMessages = File.ReadAllLines(this.InputPath);
                test = new AmazonSqsTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, queueMessages, this.InputObject, this.OutputObject);
                inputCount = queueMessages.Count();
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Blob))
            {
                AssertInput("InputPath", "FunctionName", "InputObject", "OutputObject");
                var blobs = Directory.GetFiles(this.InputPath);
                test = new AzureBlobTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, blobs, this.InputObject, this.OutputObject);
                inputCount = blobs.Count();
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Http))
            {
                AssertInput("InputPath", "FunctionName");
                var urls = File.ReadAllLines(this.InputPath);
                test = new AzureHttpTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, urls);
                inputCount = urls.Count();
            }
            else if (scenarioType.Compare(Platform.Azure, TriggerType.Queue))
            {
                AssertInput("FunctionName", "InputObject", "OutputObject");
                var queueMessages = File.ReadAllLines(this.InputPath);
                test = new AzureQueueTriggerTest(this.FunctionName, this.Eps, this.WarmUpTimeInMinutes, queueMessages, this.InputObject, this.OutputObject);
                inputCount = queueMessages.Count();
            }
            else
            {
                var ex = new ArgumentException(
                        $"Unknown combination of platform ({this.Platform}) and trigger type ({this.TriggerType}) available Platform values are ({string.Join(",", Enum.GetValues(typeof(Platform)))}) available TriggerType values are ({string.Join(",", Enum.GetValues(typeof(TriggerType)))})");
                this._logger.LogException(ex);
                throw ex;
            }
            
            profile = GetLoadProfile(inputCount);
            test.Logger = _logger;
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
                        $"Unknown load profile {this.LoadProfile} available values are ({string.Join(",", Enum.GetValues(typeof (LoadProfilesType)))})");
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
            var expcetions = new List<Exception>();

            foreach (var propertyName in properties)
            {
                var property = typeof (TestScenario).GetProperty(propertyName);
                if (property.GetValue(this) == null)
                {
                    var exception = new ArgumentException($"Required test parameter {property.Name} has not been specified.");
                    expcetions.Add(exception);
                    this._logger.LogException(exception);
                    fault = true;
                }
            }

            if (fault)
            {
                throw new AggregateException(expcetions);
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
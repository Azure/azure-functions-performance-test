using System;
using System.Collections.Generic;
using System.IO;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.TriggerTests;
using ServerlessTestOrchestrator;
using ServerlessTestOrchestrator.TestScenarios;

namespace SampleUsages
{
    internal class TestDescription
    {
        private const int InputObjectsCount = 1000;

        public string FunctionName { get; set; }
        
        public Platorm Platform { get; set; }

        public Language Language { get; set; }

        public TriggerType Trigger { get; set; }

        public FunctionType Type { get; set; }

        public string ResolveFolderName()
        {
            return string.Format(
                "{0}-{1}-{2}",
                Trigger,
                Language,
                Type);
        }

        public IFunctionsTest GenerateTestTask()
        {
            ITestScenario scenario = null;

            switch (this.Platform)
            {
                case Platorm.AzureOnly:
                    switch (this.Trigger)
                    {
                        case TriggerType.Blob:
                            scenario = new AzureBlobTestScenario();
                            break;
                        case TriggerType.Queue:
                            throw new NotImplementedException();
                        case TriggerType.Http:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentException("Unknown trigger type " + this.Trigger);
                    }

                    break;
                case Platorm.AmazonOnly:
                    throw new NotImplementedException();
                case Platorm.AzureAnyAmazon:
                    throw new NotImplementedException();
            }

            scenario.PrepareData(InputObjectsCount);
            return scenario.GetBenchmarkTest(FunctionName);
        }

        internal TriggerTestLoadProfile GenerateTestLoadProfile()
        {
            // TODO implement different load types
            return new LinearLoad(InputObjectsCount, 1);
        }
    }
}

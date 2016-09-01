using System.Collections.Generic;
using System.Linq;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonSqsTriggerTest:QueueTriggerTest
    {
        public AmazonSqsTriggerTest(string functionName, IEnumerable<string> queueMessages, string sourceBlobContainer,
            string destinationBlobContainer)
            : base(functionName, queueMessages.ToArray(), sourceBlobContainer, destinationBlobContainer)
        {
            
        }

        protected override bool Setup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
        }

        protected override void SaveCurrentProgessToDb()
        {
            // skip
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AwsController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AwsGenericPerformanceResultsProvider(); }
        }
    }
}

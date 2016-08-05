using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonSqsTriggerTest:QueueTriggerTest
    {
        public AmazonSqsTriggerTest(string functionName, IEnumerable<string> blobs, string sourceBlobContainer,
            string destinationBlobContainer)
            : base(functionName, blobs.ToArray(), sourceBlobContainer, destinationBlobContainer)
        {
            
        }

        protected override bool TestSetup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
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

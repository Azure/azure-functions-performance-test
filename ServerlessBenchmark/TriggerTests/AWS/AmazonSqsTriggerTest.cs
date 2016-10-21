using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonSqsTriggerTest:QueueTriggerTest
    {
        public AmazonSqsTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, IEnumerable<string> queueMessages, string sourceBlobContainer,
            string destinationBlobContainer)
            : base(functionName, eps, warmUpTimeInMinutes, queueMessages.ToArray(), sourceBlobContainer, destinationBlobContainer)
        {
            
        }

        protected override bool Setup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get
            {
                return new AwsController
                {
                    Logger = this.Logger
                };
            }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AwsGenericPerformanceResultsProvider { DatabaseTest = this.TestWithResults }; }
        }
    }
}

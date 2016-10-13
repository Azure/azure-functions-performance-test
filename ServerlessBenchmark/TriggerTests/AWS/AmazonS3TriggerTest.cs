using System.Collections.Generic;
using System.Linq;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonS3TriggerTest:BlobTriggerTest
    {

        public AmazonS3TriggerTest(string functionName, IEnumerable<string> blobs, string sourceBlobContainer,
            string destinationBlobContainer)
            : base(functionName, blobs.ToArray(), sourceBlobContainer, destinationBlobContainer)
        {
            
        }

        protected override bool Setup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
        }

        protected override void SaveCurrentProgessToDb()
        {
            //skip
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AwsController(this.Logger); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AwsGenericPerformanceResultsProvider(); }
        }
    }
}

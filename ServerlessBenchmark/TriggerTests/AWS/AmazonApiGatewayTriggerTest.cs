using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public class AmazonApiGatewayTriggerTest:HttpTriggerTest
    {
        public AmazonApiGatewayTriggerTest(string functionName, string[] urls) : base(functionName, urls)
        {
        }

        protected override bool Setup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
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

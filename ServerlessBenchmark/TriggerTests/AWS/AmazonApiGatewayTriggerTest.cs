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

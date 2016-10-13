using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.Azure
{
    public class AzureHttpTriggerTest:HttpTriggerTest
    {
        public AzureHttpTriggerTest(string functionName, IEnumerable<string> urls) : base(functionName, urls.ToArray())
        {

        }

        protected override bool Setup()
        {
            return Utility.RemoveAzureFunctionLogs(FunctionName,
                ConfigurationManager.AppSettings["AzureStorageConnectionString"],
                this.Logger);
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AzureController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AzureGenericPerformanceResultsProvider(); }
        }
    }
}

using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.Azure;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class FunctionTest
    {
        protected abstract bool TestSetup();
        protected abstract ICloudPlatformController CloudPlatformController { get; }
        protected abstract PerfResultProvider PerfmormanceResultProvider { get; }
        public abstract Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true);
    }
}

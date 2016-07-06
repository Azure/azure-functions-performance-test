using ServerlessBenchmark.LoadProfiles;

namespace ServerlessBenchmark.TriggerTests
{
    interface IFunctionsTest
    {
        PerfTestResult Run(TriggerTestLoadProfile loadProfile, bool warmup = true);
    }
}

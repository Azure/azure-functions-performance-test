using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;

namespace ServerlessBenchmark.TriggerTests
{
    interface IFunctionsTest
    {
        Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true);
    }
}

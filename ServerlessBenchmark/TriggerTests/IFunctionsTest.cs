namespace ServerlessBenchmark.TriggerTests
{
    interface IFunctionsTest
    {
        PerfTestResult Run(bool warmup = true);
    }
}

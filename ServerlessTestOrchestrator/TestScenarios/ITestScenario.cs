using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.TriggerTests;

namespace ServerlessTestOrchestrator
{
    interface ITestScenario
    {
        void PrepareData(int testObjectCount);
        IFunctionsTest GetBenchmarkTest(string functionName);
    }
}

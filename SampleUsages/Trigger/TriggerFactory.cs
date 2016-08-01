using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages.Trigger
{
    public class TriggerFactory
    {
        public static IFunctionsTest GetTrigger(ServerlessPlatforms platform, ServerlessTriggerTypes triggerType, string functionName, string triggerSource,
            string triggerDestination, IEnumerable<string> content)
        {
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages.Trigger
{
    public class AwsTriggerProvider:TriggerProvider 
    {
        public AwsTriggerProvider(string functionName, string triggerSource, string triggerDestination, IEnumerable<string> content) : base(functionName, triggerSource, triggerDestination, content)
        {
        }

        public override IFunctionsTest GetQueueTriggerTest()
        {
            throw new NotImplementedException();
        }

        public override IFunctionsTest GetHttpTriggerTest()
        {
            throw new NotImplementedException();
        }

        public override IFunctionsTest GetBlobTriggerTest()
        {
            return new AmazonS3TriggerTest(FunctionName, Content, TriggerSource, TriggerDestination);
        }
    }
}

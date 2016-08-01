using System.Collections.Generic;
using System.Linq;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages.Trigger
{
    public class AzureTriggerProvider:TriggerProvider
    {
        private readonly string[] _content;

        public AzureTriggerProvider(string functionName, string triggerSource, string triggerDestination, IEnumerable<string> content) : base(functionName, triggerSource, triggerDestination, content)
        {
            _content = Content.ToArray();
        }

        public override IFunctionsTest GetQueueTriggerTest()
        {
            return new AzureQueueTriggerTest(FunctionName, _content, TriggerSource, TriggerDestination);
        }

        public override IFunctionsTest GetHttpTriggerTest()
        {
            return new AzureHttpTriggerTest(FunctionName, _content);
        }

        public override IFunctionsTest GetBlobTriggerTest()
        {
            return new AzureBlobTriggerTest(FunctionName, _content, TriggerSource, TriggerDestination);
        }
    }
}

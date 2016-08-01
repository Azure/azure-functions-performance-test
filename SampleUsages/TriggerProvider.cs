using System;
using System.Collections.Generic;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages
{
    /// <summary>
    /// Each serverless platform need to extend this to provide the specific trigger tests they will like executed for each trigger type
    /// </summary>
    public abstract class TriggerProvider
    {
        protected string FunctionName { get; private set; }
        protected string TriggerSource { get; private set; }
        protected string TriggerDestination { get; private set; }
        protected IEnumerable<string> Content { get; private set; }

        private Dictionary<ServerlessTriggerTypes, Func<IFunctionsTest>> _triggerCollection; 
 
        protected TriggerProvider(string functionName, string triggerSource, string triggerDestination, IEnumerable<string> content)
        {
            FunctionName = functionName;
            TriggerSource = triggerSource;
            TriggerDestination = triggerDestination;
            Content = content;
            _triggerCollection = new Dictionary<ServerlessTriggerTypes, Func<IFunctionsTest>>
            {
                {ServerlessTriggerTypes.Blob, GetBlobTriggerTest},
                {ServerlessTriggerTypes.Queue, GetQueueTriggerTest},
                {ServerlessTriggerTypes.Http, GetHttpTriggerTest}
            };
        }

        public IFunctionsTest GetTrigger(ServerlessTriggerTypes triggerType)
        {
            return null;
        }

        public abstract IFunctionsTest GetQueueTriggerTest();
        public abstract IFunctionsTest GetHttpTriggerTest();
        public abstract IFunctionsTest GetBlobTriggerTest();
    }
}

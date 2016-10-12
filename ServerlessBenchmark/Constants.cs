using System;

namespace ServerlessBenchmark
{
    public static class Constants
    {
        public const string InsertionTime = "InsertionTime";
        public const string Topic = "Topic";
        public const string Message = "Message";
        public const string Queue = "QueueUrl";
        public const int MaxDequeueAmount = 32;

        /// <summary>
        /// The name of the table where all of the azure function execution logs are stored.
        /// </summary>
        public const string AzureFunctionLogTableName = "AzureFunctionsLogTable";

        /// <summary>
        /// Partition key of the execution logs that carries information such as container and etc.
        /// </summary>
        public const string AzureFunctionLogExecutionPartitionKey = "R";

        /// <summary>
        /// Timeout in milliseconds for each http request
        /// </summary>
        public const int HttpTriggerTimeoutMilliseconds = 300 * 1000;

        public const int LoadCoolDownTimeout = 180 * 1000;
    }

    public enum TriggerTypes { Blob, Queue, Http}

    public enum Platform { Azure, Amazon }
}

namespace ServerlessBenchmark
{
    public static class Constants
    {
        public const string InsertionTime = "InsertionTime";
        public const string Topic = "Topic";
        public const string Message = "Message";
        public const string Queue = "QueueUrl";
        public const int MaxDequeueAmount = 32;
    }

    public enum ServerlessPlatforms { Azure, Aws }
    public enum ServerlessTriggerTypes { Blob, Queue, Http}
    public enum DataRequestKeys { Message, Other}
}

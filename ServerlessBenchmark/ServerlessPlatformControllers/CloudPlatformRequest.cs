using System;
using System.Collections.Generic;
using System.IO;

namespace ServerlessBenchmark.ServerlessPlatformControllers
{
    public class CloudPlatformRequest
    {
        public DateTime TimeStamp { get; set; }
        public int RequestId { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public string Source { get; set; }
        public Stream DataStream { get; set; }
        public string Key { get; set; }
        public int MessageCount { get; set; }

    }
}

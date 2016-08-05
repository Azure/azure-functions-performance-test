using System;
using System.Collections.Generic;

namespace ServerlessBenchmark.ServerlessPlatformControllers
{
    public class CloudPlatformResponse
    {
        public int ResponseId { get; set; }
        public System.Net.HttpStatusCode HttpStatusCode { get; set; }
        public DateTime TimeStamp { get; set; }
        public Dictionary<string, DateTime> TimeStamps { get; set; } 
        public object Data { get; set; }

        public Dictionary<string, int> ErrorDetails { get; private set; }

        public CloudPlatformResponse()
        {
            ErrorDetails = new Dictionary<string, int>();
        }
    }
}

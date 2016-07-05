using System.Collections.Generic;

namespace ServerlessBenchmark
{
    public class TestRequest
    {
        private const int DefaultRetries = 5;
        private int _retries = 0;
        public IEnumerable<string> Messages { get; set; }
        public string SrcQueue { get; set; }
        public string DstQueue { get; set; }
        public int[] DistributedBatchMessageSizes { get; set; }
        public string CloudPlatform { get; set; }

        public int Retries
        {
            get
            {
                if (_retries == 0)
                {
                    return DefaultRetries;
                }
                return _retries;
            }
            set { _retries = value; }
        }
    }
}

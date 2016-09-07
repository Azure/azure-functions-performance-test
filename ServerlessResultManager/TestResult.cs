namespace ServerlessResultManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class TestResult
    {
        public long Id { get; set; }

        public long CallCount { get; set; }

        public long SuccessCount { get; set; }

        public long FailedCount { get; set; }

        public long TimeoutCount { get; set; }

        public double AverageLatency { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime Timestamp { get; set; }

        public long TestId { get; set; }

        public virtual Test Test { get; set; }
    }
}

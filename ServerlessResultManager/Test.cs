namespace ServerlessResultManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Test
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Test()
        {
            TestResults = new HashSet<TestResult>();
        }

        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime StartTime { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? EndTime { get; set; }

        [Required]
        [StringLength(200)]
        public string Platform { get; set; }

        [Required]
        public string Description { get; set; }

        [StringLength(200)]
        public string Owner { get; set; }

        [StringLength(250)]
        public string ToolsVersion { get; set; }

        [StringLength(250)]
        public string PlatformVersion { get; set; }

        [StringLength(250)]
        public string FunctionsRuntimeVersion { get; set; }

        public int? TargetEps { get; set; }

        [StringLength(250)]
        public string TriggerType { get; set; }

        [StringLength(250)]
        public string Source { get; set; }

        [StringLength(250)]
        public string Destination { get; set; }

        public double? AverageExecutionTime { get; set; }

        public int? ExecutionCount { get; set; }

        public double? ExecutionTimeStandardDeviation { get; set; }

        public double? FunctionClockTime { get; set; }

        public double? HostConcurrency { get; set; }

        public double? Throughput { get; set; }

        public int? Errors { get; set; }

        public long? TestScenarioId { get; set; }

        public virtual TestScenario TestScenario { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TestResult> TestResults { get; set; }
    }
}

namespace ServerlessResultManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class TestScenario
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TestScenario()
        {
            Tests = new HashSet<Test>();
        }

        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime StartTimeUtc { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? EndTimeUtc { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Test> Tests { get; set; }
    }
}

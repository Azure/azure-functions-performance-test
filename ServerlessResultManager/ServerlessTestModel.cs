using System.Configuration;
using System.Data.Entity.Infrastructure;

namespace ServerlessResultManager
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ServerlessTestModel : DbContext
    {
        public ServerlessTestModel(string connectionString)
            : base(connectionString)
        {
        }

        public static string GetConnectionString()
        {
            var connString = System.Configuration.ConfigurationManager.AppSettings["DbConnectionString"];
            return connString;
        }

        public virtual DbSet<TestScenario> TestScenarios { get; set; }
        public virtual DbSet<TestResult> TestResults { get; set; }
        public virtual DbSet<Test> Tests { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}

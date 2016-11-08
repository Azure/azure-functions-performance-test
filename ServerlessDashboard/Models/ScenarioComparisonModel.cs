using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServerlessResultManager;

namespace ServerlessDashboard.Models
{
    public class ScenarioComparisonModel
    {
        public TestScenario FirstScenario { get; set; }
        public TestScenario SecondScenario { get; set; }
        public IList<Test> FirstScenarioTests { get; set; }
        public IList<Test> SecondScenarioTests { get; set; }

        public ScenarioComparisonModel(TestScenario firstScenario, TestScenario secondScenario)
        {
            this.FirstScenario = firstScenario;
            this.SecondScenario = secondScenario;
            this.FirstScenarioTests = firstScenario.Tests.ToList();
            this.SecondScenarioTests = secondScenario.Tests.ToList();
        }
    }
}
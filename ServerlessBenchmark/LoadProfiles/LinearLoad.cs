using System;

namespace ServerlessBenchmark.LoadProfiles
{
    /// <summary>
    /// This load profile just follows the basic definition of linear. 
    /// y = mx + b
    /// </summary>
    public class LinearLoad:TriggerTestLoadProfile
    {
        public LinearLoad(TimeSpan loadDuration, int totalNumberOfPostItems) : base(loadDuration, totalNumberOfPostItems)
        {
        }

        protected override int ExecuteRate(int t)
        {
            int durationInSeconds = (int) LoadDuration.TotalSeconds;
            int yIntercept = TotalNumberOfPostItems/durationInSeconds;
            yIntercept = yIntercept == 0 ? 1 : yIntercept;
            int slope = 0;
            return slope*t + yIntercept;
        }
    }
}

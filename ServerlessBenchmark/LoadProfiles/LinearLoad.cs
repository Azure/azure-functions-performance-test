using System;
using System.Threading;

namespace ServerlessBenchmark.LoadProfiles
{
    /// <summary>
    /// This load profile just follows the basic definition of linear. 
    /// y = mx + b
    /// </summary>
    public class LinearLoad:TriggerTestLoadProfile
    {
        private int _totalNumberOfPostItems;
        private readonly int _targetEps;
        private int _isFinished;

        /// <summary>
        /// Given some duration, run linear pattern load against a function.
        /// </summary>
        /// <param name="loadDuration"></param>
        /// <param name="eps">This is target executions per second</param>
        public LinearLoad(TimeSpan loadDuration, int eps) : base(loadDuration)
        {
            _targetEps = eps;
        }

        /// <summary>
        /// Run linear pattern load against a function.
        /// </summary>
        /// <param name="totalNumberOfPostItems"></param>
        /// <param name="eps">This is target executions per second</param>
        public LinearLoad(int totalNumberOfPostItems, int eps) : base(TimeSpan.FromMilliseconds(Int32.MaxValue))
        {
            _targetEps = eps;
            _totalNumberOfPostItems = totalNumberOfPostItems;
        }

        protected override int ExecuteRate(int t)
        {
            if (_totalNumberOfPostItems > 0 && _targetEps > 0)
            {
                _totalNumberOfPostItems = Math.Max(_targetEps, _totalNumberOfPostItems) - Math.Min(_targetEps, _totalNumberOfPostItems);
                if (_totalNumberOfPostItems <= 0)
                {
                    Interlocked.Increment(ref _isFinished);
                }
            }
            const int slope = 0;
            return slope*t + _targetEps;
        }

        protected override bool IsFinished()
        {
            return _isFinished == 1;
        }
    }
}

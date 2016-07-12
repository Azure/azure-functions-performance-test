//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.Threading;

namespace ServerlessBenchmark.LoadProfiles
{
    /// <summary>
    /// Base class for load profiles
    /// </summary>
    public abstract class TriggerTestLoadProfile:IDisposable
    {
        protected int TotalNumberOfPostItems;
        protected TimeSpan LoadDuration;
        private Timer _calculateRateTimer, _calculateRateStopper;
        private bool _isCalculateRateTimerStopped;
        private int _timeCounter = -1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadDuration"></param>
        /// <param name="totalNumberOfPostItems">Number of items posted to queue, event hub, blob container, etc.</param>
        protected TriggerTestLoadProfile(TimeSpan loadDuration, int totalNumberOfPostItems)
        {
            TotalNumberOfPostItems = totalNumberOfPostItems;
            LoadDuration = loadDuration;
        }

        /// <summary>
        /// Given a duration and <see href="https://msdn.microsoft.com/en-us/library/018hxwa8(v=vs.110).aspx">action</see>, calculate rate of publishing items and execute the given action.
        /// </summary>
        /// <param name="action"></param>
        public void ExecuteRate(Action<int> action)
        {
            int durationInSeconds = (int)LoadDuration.TotalSeconds;
            TimerCallback callback = t =>
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                action(rate);
            };
            TimerCallback calculateRateTimerStopper = state =>
            {
                if (_timeCounter >= durationInSeconds)
                {
                    _calculateRateTimer.Dispose();
                    _isCalculateRateTimerStopped = true;
                }
            };
            _calculateRateTimer = new Timer(callback, null, 0, 1000 * 1);
            _calculateRateStopper = new Timer(calculateRateTimerStopper, null, 0, 1100);
        }

        protected abstract int ExecuteRate(int t);

        public void Dispose()
        {
            if (!_isCalculateRateTimerStopped)
            {
                _calculateRateTimer.Dispose();
            }
            _calculateRateStopper.Dispose();
        }
    }
}

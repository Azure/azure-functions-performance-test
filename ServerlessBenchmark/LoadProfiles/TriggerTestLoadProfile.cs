//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServerlessBenchmark.LoadProfiles
{
    /// <summary>
    /// Base class for load profiles
    /// </summary>
    public abstract class TriggerTestLoadProfile:IDisposable
    {
        protected TimeSpan LoadDuration;
        private Timer _calculateRateTimer;
        private int _timeCounter = -1;

        protected TriggerTestLoadProfile(TimeSpan loadDuration)
        {
            LoadDuration = loadDuration;
        }

        /// <summary>
        /// Given a duration and <see href="https://msdn.microsoft.com/en-us/library/018hxwa8(v=vs.110).aspx">action</see>, calculate rate of publishing items and execute the given action.
        /// </summary>
        /// <param name="action"></param>
        public async Task ExecuteRateAsync(Func<int, Task> action)
        {
            TimerCallback callback = async t =>
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                await action(rate);
            };
            _calculateRateTimer = new Timer(callback, null, 0, 1000 * 1);
            var isLoadFinishedTask = Task.Run(() =>
            {
                while (!IsFinished())
                {
                    //keep cycling
                    Task.Delay(TimeSpan.FromSeconds(1));
                }
            });

            var loadDurationTimerTask = Task.Delay(LoadDuration + TimeSpan.FromSeconds(1));

            await Task.WhenAny(loadDurationTimerTask, isLoadFinishedTask);
        }

        /// <summary>
        /// Given a duration and <see href="https://msdn.microsoft.com/en-us/library/018hxwa8(v=vs.110).aspx">action</see>, calculate rate of publishing items and execute the given action.
        /// </summary>
        /// <param name="action"></param>
        public async Task ExecuteRateAsync(Func<int, Task> action, int limit = 1000)
        {
            TimerCallback callback = async t =>
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                await action(rate);
            };
            _calculateRateTimer = new Timer(callback, null, 0, 1000 * 1);
            var isLoadFinishedTask = Task.Run(() =>
            {
                while (!IsFinished())
                {
                    //keep cycling
                    Task.Delay(TimeSpan.FromSeconds(1));
                }
            });

            for (int i = 0; i < Int32.MaxValue; i++)
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                await action(rate);
            }

            var loadTasks = new List<Task>();
            while (true)
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                var t = action(rate);
                if (loadTasks.Count < limit)
                {
                    
                }
            }

            var loadDurationTimerTask = Task.Delay(LoadDuration + TimeSpan.FromSeconds(1));

            await Task.WhenAny(loadDurationTimerTask, isLoadFinishedTask);
        }

        protected abstract int ExecuteRate(int t);

        protected abstract bool IsFinished();

        public void Dispose()
        {
            _calculateRateTimer.Dispose();
        }
    }
}

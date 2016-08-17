//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private List<Task> _runningTasks;

        protected TriggerTestLoadProfile(TimeSpan loadDuration)
        {
            LoadDuration = loadDuration;
            _runningTasks = new List<Task>();
        }

        /// <summary>
        /// Given a duration and <see href="https://msdn.microsoft.com/en-us/library/018hxwa8(v=vs.110).aspx">action</see>, calculate rate of publishing items and execute the given action.
        /// </summary>
        /// <param name="action"></param>
        public async Task ExecuteRateAsync(Func<int, Task> action, bool enableLoadCoolDown = false)
        {
            TimerCallback callback = async t =>
            {
                Interlocked.Increment(ref _timeCounter);
                int rate = ExecuteRate(_timeCounter);
                var loadAction = action(rate);
                _runningTasks.Add(loadAction);
                await loadAction;
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
            if (enableLoadCoolDown)
            {
                Console.WriteLine("Cool down");
                Dispose();
                var outstandingTasks = _runningTasks.Where(t => !t.IsCompleted);
                while (true)
                {
                    var tmp = outstandingTasks.Where(t => !t.IsCompleted);
                    if (!tmp.Any())
                    {
                        break;
                    }
                    Console.WriteLine("Outstanding Requests:    {0}", tmp.Count());
                    await Task.Delay(500);
                }
            }
        }

        protected abstract int ExecuteRate(int t);

        protected abstract bool IsFinished();

        public void Dispose()
        {
            _calculateRateTimer.Dispose();
        }
    }
}

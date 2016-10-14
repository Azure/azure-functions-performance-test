using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessResultManager;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    /// <summary>
    /// Base HTTP function that all platform HTTP type functions can inherit from
    /// </summary>
    public abstract class HttpTriggerTest : FunctionTest
    {
        protected override sealed IEnumerable<string> SourceItems { get; set; }
        private int _totalSuccessRequests;
        private int _totalSuccessRequestsWithTick;
        private int _totalFailedRequests;
        private int _totalFailedRequestsWithTick;
        private int _totalTimedOutRequests;
        private int _totalTimedOutRequestsWithTick;
        private int _totalActiveRequests;
        private int _totalActiveRequestsWithTick;
        private int _totalRequests;
        private int _totalRequestsWithTick;
        private readonly int _tickTimeInMiliseconds = 1000;
        private readonly ConcurrentBag<int> _responseTimes;
        private readonly ConcurrentDictionary<int, Task<HttpResponseMessage>> _runningTasks;
        private readonly List<Task> _loadRequests;

        protected abstract bool Setup();

        protected HttpTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, string[] urls):base(functionName, eps, warmUpTimeInMinutes)
        {
            if (!urls.Any())
            {
                throw new ArgumentException("Urls is empty", "urls");
            }
            if (String.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be empty", "functionName");
            }

            SourceItems = urls.ToList();
            _runningTasks = new ConcurrentDictionary<int, Task<HttpResponseMessage>>();
            _loadRequests = new List<Task>();
            _responseTimes = new ConcurrentBag<int>();

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
        }

        protected override bool TestSetupWithRetry()
        {
            return Setup();
        }

        protected abstract override PerfResultProvider PerfmormanceResultProvider { get; }

        protected override Task PreReportGeneration(DateTime testStartTime, DateTime testEndTime)
        {
            return Task.FromResult(0);
        }

        protected async override Task Load(IEnumerable<string> requestItems)
        {
            await LoadSites(requestItems);
        }

        private async Task LoadSites(IEnumerable<string> sites)
        {
            var client = new HttpClient();
            var loadRequests = new List<Task>();
            // for dev environments skip certificate validation
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            foreach (var site in sites)
            {
                var t = Task.Run(async () =>
                {
                    DateTime requestSent = new DateTime();
                    try
                    {
                        var cs = new CancellationTokenSource(Constants.HttpTriggerTimeoutMilliseconds);
                        var request = client.GetAsync(site, cs.Token);
                        requestSent = DateTime.Now;
                        Interlocked.Increment(ref _totalActiveRequests);
                        Interlocked.Increment(ref _totalRequests);
                        if (!_runningTasks.TryAdd(request.Id, request))
                        {
                            Logger.LogWarning("Error on tracking this task");
                        }
                        var response = await request;
                        var responseReceived = DateTime.Now;
                        Interlocked.Decrement(ref _totalActiveRequests);
                        _responseTimes.Add((int)(responseReceived - requestSent).TotalMilliseconds);
                        if (response.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref _totalSuccessRequests);
                        }
                        else
                        {
                            Interlocked.Increment(ref _totalFailedRequests);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _responseTimes.Add((int)(DateTime.Now - requestSent).TotalMilliseconds);
                        Interlocked.Increment(ref _totalTimedOutRequests);
                        Interlocked.Decrement(ref _totalActiveRequests);
                    }
                });
                _loadRequests.Add(t);
            }
            await Task.WhenAll(loadRequests);
        }

        protected override async Task TestCoolDown()
        {
            if (_runningTasks.Any())
            {
                var lastSize = 0;
                DateTime lastNewSize = new DateTime();
                string testProgressString;
                while (true)
                {
                    try
                    {
                        if (!this.DuringWarmUp)
                        {
                            this.SaveCurrentProgessToDb();
                        }

                        testProgressString = PrintTestProgress();
                        testProgressString = $"OutStanding:    {_totalActiveRequests}     {testProgressString}";

                        if (_totalActiveRequests == 0)
                        {
                            Logger.LogInfo(testProgressString);
                            Logger.LogInfo("Finished Outstanding Requests");
                            break;
                        }

                        if (_totalActiveRequests != lastSize)
                        {
                            lastSize = _totalActiveRequests;
                            lastNewSize = DateTime.Now;
                        }
                        else
                        {
                            var secondsSinceLastNewSize = (DateTime.Now - lastNewSize).TotalSeconds;
                            var secondsLeft = TimeSpan.FromMilliseconds(Constants.LoadCoolDownTimeout).TotalSeconds - secondsSinceLastNewSize;
                            Logger.LogInfo("No new requests for {0} seconds. Waiting another {1}s to finish", secondsSinceLastNewSize, secondsLeft);

                            if (secondsLeft < 0)
                            {
                                break;
                            }
                        }

                        Logger.LogInfo(testProgressString);
                        await Task.Delay(_tickTimeInMiliseconds);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e);
                    }
                }
            }

            // clean after cool down
            _totalSuccessRequests = 0;
            _totalSuccessRequestsWithTick = 0;
            _totalFailedRequests = 0;
            _totalFailedRequestsWithTick = 0;
            _totalTimedOutRequests = 0;
            _totalTimedOutRequestsWithTick = 0;
            _totalActiveRequests = 0;
            _totalActiveRequestsWithTick = 0;
            _totalRequests = 0;
            _totalRequestsWithTick = 0;
        }

        protected override void SaveCurrentProgessToDb()
        {
            var totalRequests = _totalRequests;
            var totalSuccessRequests = _totalSuccessRequests;
            var totalFailedRequests = _totalFailedRequests;
            var totalTimeoutRequests = _totalTimedOutRequests;

            var progressResult = new TestResult
            {
                Timestamp = DateTime.UtcNow,
                CallCount = totalRequests - _totalRequestsWithTick,
                FailedCount = totalFailedRequests - _totalFailedRequestsWithTick,
                SuccessCount = totalSuccessRequests - _totalSuccessRequestsWithTick,
                TimeoutCount = totalTimeoutRequests - _totalTimedOutRequestsWithTick,
                AverageLatency = _responseTimes.IsEmpty ? .0 : _responseTimes.Average()
            };

            this.TestRepository.AddTestResult(this.TestWithResults, progressResult);
            _totalRequestsWithTick = totalRequests;
            _totalFailedRequestsWithTick = totalFailedRequests;
            _totalSuccessRequestsWithTick = totalSuccessRequests;
            _totalTimedOutRequestsWithTick = _totalTimedOutRequests;
        }

        protected override IDictionary<string, string> CurrentTestProgress()
        {
            var testProgressData = base.CurrentTestProgress() ?? new Dictionary<string, string>();
            testProgressData.Add("Success", _totalSuccessRequests.ToString());
            testProgressData.Add("Failed", _totalFailedRequests.ToString());
            testProgressData.Add("Timeout", _totalTimedOutRequests.ToString());
            testProgressData.Add("Active", _totalActiveRequests.ToString());
            testProgressData.Add("AvgLatency(ms)", _responseTimes.IsEmpty ? 0.ToString() : _responseTimes.Average().ToString());
            //reset values
            ResetHttpCounters();

            return testProgressData;
        }

        private void ResetHttpCounters()
        {
            int t;
            while (!_responseTimes.IsEmpty)
            {
                _responseTimes.TryTake(out t);
            }
        }
    }
}

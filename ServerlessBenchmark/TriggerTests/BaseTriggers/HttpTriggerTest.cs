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

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    /// <summary>
    /// Base HTTP function that all platform HTTP type functions can inherit from
    /// </summary>
    public abstract class HttpTriggerTest : FunctionTest
    {
        protected override sealed IEnumerable<string> SourceItems { get; set; }
        private int _totalSuccessRequestsPerSecond;
        private int _totalFailedRequestsPerSecond;
        private int _totalTimedOutRequestsPerSecond;
        private int _totalActiveRequests;
        private readonly ConcurrentBag<int> _responseTimes;
        private readonly ConcurrentDictionary<int, Task<HttpResponseMessage>> _runningTasks;
        private readonly List<Task> _loadRequests;

        protected abstract bool Setup();

        protected HttpTriggerTest(string functionName, string[] urls):base(functionName)
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

        protected override Task TestWarmup()
        {
            var warmSite = SourceItems.First();
            var client = new HttpClient();
            var cs = new CancellationTokenSource(Constants.HttpTriggerTimeoutMilliseconds);
            HttpResponseMessage response;
            try
            {
                response = client.GetAsync(warmSite, cs.Token).Result;
            }
            catch (TaskCanceledException)
            {
                throw new Exception(String.Format("Warm up passed timeout of {0}ms", Constants.HttpTriggerTimeoutMilliseconds));
            }

            var isSuccessfulSetup = response.IsSuccessStatusCode;
            return Task.FromResult(isSuccessfulSetup);
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
                        if (!_runningTasks.TryAdd(request.Id, request))
                        {
                            Console.WriteLine("Error on tracking this task");
                        }
                        var response = await request;
                        var responseReceived = DateTime.Now;
                        Interlocked.Decrement(ref _totalActiveRequests);
                        _responseTimes.Add((int)(responseReceived - requestSent).TotalMilliseconds);
                        if (response.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref _totalSuccessRequestsPerSecond);
                        }
                        else
                        {
                            Interlocked.Increment(ref _totalFailedRequestsPerSecond);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _responseTimes.Add((int)(DateTime.Now - requestSent).TotalMilliseconds);
                        Interlocked.Increment(ref _totalTimedOutRequestsPerSecond);
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
                        testProgressString = PrintTestProgress();
                        testProgressString = $"OutStanding:    {_totalActiveRequests}     {testProgressString}";

                        if (_totalActiveRequests == 0)
                        {
                            Console.WriteLine(testProgressString);
                            Console.WriteLine("Finished Outstanding Requests");
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
                            Console.WriteLine("No new requests for {0} seconds. Waiting another {1}s to finish", secondsSinceLastNewSize, secondsLeft);

                            if (secondsLeft < 0)
                            {
                                break;
                            }
                        }
                        
                        Console.WriteLine(testProgressString);
                        await Task.Delay(1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        protected override IDictionary<string, string> CurrentTestProgress()
        {
            var testProgressData = base.CurrentTestProgress() ?? new Dictionary<string, string>();
            testProgressData.Add("Success", _totalSuccessRequestsPerSecond.ToString());
            testProgressData.Add("Failed", _totalFailedRequestsPerSecond.ToString());
            testProgressData.Add("Timeout", _totalTimedOutRequestsPerSecond.ToString());
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

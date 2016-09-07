using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessResultManager;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class FunctionTest
    {
        protected string FunctionName { get; set; }
        protected abstract IEnumerable<string> SourceItems { get; set; }
        protected int ExpectedExecutionCount;
        private int _executionsPerSecond;
        protected abstract bool TestSetupWithRetry();
        protected abstract Task TestWarmup();
        protected abstract Task TestCoolDown();
        protected abstract Task PreReportGeneration(DateTime testStartTime, DateTime testEndTime);
        protected abstract void SaveCurrentProgessToDb();
        protected abstract ICloudPlatformController CloudPlatformController { get; }
        protected abstract PerfResultProvider PerfmormanceResultProvider { get; }
        private bool onTestCoolDown = false;
        protected Test TestWithResults { get; set; }
        protected ITestRepository TestRepository { get; set; }

        protected FunctionTest(string functionName)
        {
            FunctionName = functionName;
            TestRepository = new TestRepository();
        }

        public async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            var retries = 3;
            bool isSuccessSetup;
            do
            {
                isSuccessSetup = TestSetupWithRetry();
                retries = retries - 1;
            } while (retries > 0 && !isSuccessSetup);

            if (warmup)
            {
                await TestWarmup();
            }

            Console.WriteLine("--START-- Running load");
            var startTime = DateTime.Now;

            this.TestWithResults = new Test
            {
                StartTime = startTime.ToUniversalTime(),
                Name = $"Test function - {FunctionName}",
                Platform = CloudPlatformController.PlatformName.ToString(),
                Description = "Test manually run from console app."
            };

            this.TestWithResults = this.TestRepository.AddTest(this.TestWithResults);
            var sw = Stopwatch.StartNew();
            await loadProfile.ExecuteRateAsync(GenerateLoad);
            loadProfile.Dispose();
            onTestCoolDown = true;
            await TestCoolDown();
            sw.Stop();
            var clientEndTime = DateTime.Now;
            this.TestWithResults.EndTime = clientEndTime.ToUniversalTime();
            this.TestRepository.UpdateTest(this.TestWithResults);
            Console.WriteLine("--END-- Elapsed time:      {0}", sw.Elapsed);
            await PreReportGeneration(startTime, clientEndTime);
            var perfResult = PerfmormanceResultProvider.GetPerfMetrics(FunctionName, startTime, clientEndTime, expectedExecutionCount: ExpectedExecutionCount);
            return perfResult;
        }

        protected abstract Task Load(IEnumerable<string> requestItems);

        protected async Task GenerateLoad(int requests)
        {
            var srcNumberOfItems = SourceItems.Count();
            List<string> selectedItems;
            var randomResources = SourceItems.OrderBy(i => Guid.NewGuid());
            if (requests <= srcNumberOfItems)
            {
                selectedItems = randomResources.Take(requests).ToList();
            }
            else
            {
                var tmpList = new List<string>();
                do
                {
                    tmpList.AddRange(randomResources.Take(requests));
                    requests -= srcNumberOfItems;
                } while (requests >= 0);
                selectedItems = tmpList;
            }
            _executionsPerSecond = selectedItems.Count;
            SaveCurrentProgessToDb();
            Console.WriteLine(PrintTestProgress());
            Interlocked.Add(ref ExpectedExecutionCount, selectedItems.Count());
            await Load(selectedItems);
        }

        protected virtual string PrintTestProgress(Dictionary<string, string> testProgress = null)
        {
            var sb = new StringBuilder();
            var progressData = testProgress ?? CurrentTestProgress();
            sb.Append(DateTime.Now);
            foreach (var data in progressData)
            {
                sb.AppendFormat("   {0}:   {1}", data.Key, data.Value);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        protected virtual IDictionary<string, string> CurrentTestProgress()
        {
            Dictionary<string, string> progressData = null;
            if (!onTestCoolDown)
            {
                progressData = new Dictionary<string, string>
                {
                    {"EPS", _executionsPerSecond.ToString()}
                };
            }
            return progressData;
        } 
    }
}

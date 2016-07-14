using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests
{
    public abstract class BlobTriggerTest:IFunctionsTest
    {
        protected string[] BlobPaths;
        protected readonly string SrcBlobContainer;
        protected readonly string DstBlobContainer;
        protected readonly string FunctionName;
        private readonly string _tmpBlobContent = "Hi this is a warm up test";
        private int expectedDestinationBlobContainerCount = 0;

        protected abstract bool TestSetup();
        protected abstract ICloudPlatformController CloudPlatformController { get; }
        protected abstract PerfResultProvider PerfmormanceResultProvider { get; }

        protected virtual string WarmUpBlob
        {
            get
            {
                return BlobPaths.First();
            }
        }

        protected BlobTriggerTest(string functionName, string[] blobs, string sourceBlobContainer, string destinationBlobContainer)
        {
            if (string.IsNullOrEmpty(functionName) || string.IsNullOrEmpty(sourceBlobContainer) || string.IsNullOrEmpty(destinationBlobContainer)
                || blobs == null)
            {
                throw new ArgumentException("Not all of the arguments are met");
            }

            BlobPaths = blobs;
            DstBlobContainer = destinationBlobContainer;
            SrcBlobContainer = sourceBlobContainer;
            FunctionName = functionName;
        }

        protected BlobTriggerTest()
        {
            
        }

        protected virtual bool SetUp(int retries = 3)
        {
            bool successfulSetup;
            Console.WriteLine("Blog trigger tests - setup");
            try
            {
                Console.WriteLine("Deleting blobs");
                var cloudPlatformResponses = new List<CloudPlatformResponse>
                {
                    {CloudPlatformController.DeleteBlobs(new CloudPlatformRequest() {Source = SrcBlobContainer})},
                    {CloudPlatformController.DeleteBlobs(new CloudPlatformRequest() {Source = DstBlobContainer})}
                };
                var undoneJobs = cloudPlatformResponses.Where(response => response != null && response.HttpStatusCode != HttpStatusCode.OK);
                successfulSetup = !undoneJobs.Any();
                if (!successfulSetup && retries > 0)
                {
                    retries = retries - 1;
                    SetUp(retries);
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Could not setup {0} Test", "Blob Trigger"), e);
            }
            var isSuccessTestSetup = TestSetup();
            return successfulSetup & isSuccessTestSetup;
        }

        protected virtual void TestWarmUp()
        {
            Console.WriteLine("Blog Trigger Warmup - Starting");

            if (CloudPlatformController == null)
            {
                throw new NullReferenceException("Make sure the cloud platform is initialized");
            }

            Console.WriteLine("Blog Trigger Warmup - using first item in blobs {0}", WarmUpBlob);

            var sw = Stopwatch.StartNew();

            Console.WriteLine("Blog Trigger Warmup - Posting {0} to cloud platform", WarmUpBlob);

            UploadBlobs(new[] { WarmUpBlob });

            Console.WriteLine("Blog Trigger Warmup - Verify test blob is there:");

            bool isWarmUpSuccess = VerifyBlobItemsExistInTargetDestination(1);

            sw.Stop();

            Console.WriteLine("Blog Trigger Warmup - Clean Up");

            SetUp();

            Console.WriteLine(isWarmUpSuccess ? "Blog Trigger Warmup - Done!" : "Blog Trigger Warmup - Done with failures");
            Console.WriteLine("Blog Trigger Warmup - Elapsed Time: {0}ms", sw.ElapsedMilliseconds);

            if (!isWarmUpSuccess)
            {
                throw new Exception("Could not find temporary blob file in target container");
            }
        }

        public async Task<PerfTestResult> RunAsync(TriggerTestLoadProfile loadProfile, bool warmup = true)
        {
            int blobCount = BlobPaths.Count();
            DateTime clientStartTime, clientEndTime;

            if (CloudPlatformController == null)
            {
                throw new NullReferenceException("Make sure the cloud platform is initialized");
            }
            if (SetUp())
            {
                if (warmup)
                {
                    TestWarmUp();
                }

                Console.WriteLine("Posting Blobs");
                clientStartTime = DateTime.Now;
                var sw = Stopwatch.StartNew();
                await loadProfile.ExecuteRateAsync(UploadBlobs);
                loadProfile.Dispose();
                sw.Stop();
                Console.WriteLine("Elapsed time to post blobs:      {0}", sw.Elapsed);
            }
            else
            {
                throw new Exception("Could not successfully setup Blob Trigger Test");
            }

            Console.WriteLine("Verify all blobs are there:");
            VerifyBlobItemsExistInTargetDestination(blobCount);

            clientEndTime = DateTime.Now;
            var perfResult = PerfmormanceResultProvider.GetPerfMetrics(FunctionName, clientStartTime, clientEndTime, expectedExecutionCount: blobCount);
            return perfResult;
        }

        private void UploadBlobs(int targetNumberOfBlobItems)
        {
            int srcNumberOfBlobItems = BlobPaths.Count();
            IEnumerable<string> selectedBlobs;
            if (targetNumberOfBlobItems <= srcNumberOfBlobItems)
            {
                selectedBlobs = BlobPaths.Take(targetNumberOfBlobItems);
            }
            else
            {
                var tmpBlobList = new List<string>();
                do
                {
                    tmpBlobList.AddRange(BlobPaths.Take(targetNumberOfBlobItems));
                    targetNumberOfBlobItems -= srcNumberOfBlobItems;
                } while (targetNumberOfBlobItems >= 0);
                selectedBlobs = tmpBlobList;
            }
            UploadBlobs(selectedBlobs);
            Console.WriteLine("EPS = {0} {1}", selectedBlobs.Count(), DateTime.Now);
            Interlocked.Add(ref expectedDestinationBlobContainerCount, selectedBlobs.Count());
        }

        private void UploadBlobs(IEnumerable<string> blobs)
        {
            Parallel.ForEach(blobs, blobPath =>
            {
                using (FileStream stream = new FileStream(blobPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    CloudPlatformController.PostBlob(new CloudPlatformRequest()
                    {
                        Key = Guid.NewGuid().ToString(),
                        Source = SrcBlobContainer,
                        DataStream = stream
                    });
                }
            });
        }

        private bool VerifyBlobItemsExistInTargetDestination(int expected)
        {
            IEnumerable<object> blobs;
            do
            {
                blobs = (IEnumerable<object>)CloudPlatformController.ListBlobs(new CloudPlatformRequest()
                {
                    Source = DstBlobContainer
                }).Data;
                Console.WriteLine("Destination Blobs - Number Of Blobs:     {0}", blobs.Count());
                Thread.Sleep(1 * 1000);
            } while (blobs.Count() < expected);
            return true;
        }
    }
}

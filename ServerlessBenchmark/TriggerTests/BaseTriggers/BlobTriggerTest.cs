using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests.BaseTriggers
{
    public abstract class BlobTriggerTest:StorageTriggerTest
    {
        protected readonly string SrcBlobContainer;
        protected readonly string DstBlobContainer;
        protected readonly string _functionName;

        protected BlobTriggerTest(string functionName, int eps, int warmUpTimeInMinutes, string[] blobs, string sourceBlobContainer, string destinationBlobContainer):base(functionName, eps, warmUpTimeInMinutes, blobs)
        {
            if (string.IsNullOrEmpty(functionName) || string.IsNullOrEmpty(sourceBlobContainer) || string.IsNullOrEmpty(destinationBlobContainer)
                || blobs == null)
            {
                throw new ArgumentException("Not all of the arguments are met");
            }

            DstBlobContainer = destinationBlobContainer;
            SrcBlobContainer = sourceBlobContainer;
            _functionName = functionName;
        }

        public BlobTriggerTest():base(null, 0, 0, null) { }

        protected virtual string WarmUpBlob
        {
            get
            {
                return SourceItems.First();
            }
        }

        protected override string StorageType
        {
            get { return "Blob"; }
        }

        protected override List<CloudPlatformResponse> CleanUpStorageResources()
        {
            var cloudPlatformResponses = new List<CloudPlatformResponse>
                {
                    {CloudPlatformController.DeleteBlobs(new CloudPlatformRequest() {Source = SrcBlobContainer})},
                    {CloudPlatformController.DeleteBlobs(new CloudPlatformRequest() {Source = DstBlobContainer})}
                };
            return cloudPlatformResponses;
        }

        protected override async Task UploadItems(IEnumerable<string> items)
        {
            await UploadBlobs(items);
        }

        protected override Task<bool> VerifyTargetDestinationStorageCount(int expectedCount)
        {
            return Task.FromResult(VerifyBlobItemsExistInTargetDestination(expectedCount));
        }

        private async Task UploadBlobs(IEnumerable<string> blobs)
        {
            Parallel.ForEach(blobs, async blobPath =>
            {
                using (FileStream stream = new FileStream(blobPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await CloudPlatformController.PostBlobAsync(new CloudPlatformRequest()
                    {
                        Key = Guid.NewGuid().ToString(),
                        Source = SrcBlobContainer,
                        DataStream = stream
                    });
                }
            });
        }

        protected override Task TestCoolDown()
        {
            return Task.FromResult(true);
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
                this.Logger.LogInfo("Destination Blobs - Number Of Blobs:     {0}", blobs.Count());
                Thread.Sleep(1 * 1000);
            } while (blobs.Count() < expected);
            return true;
        }
    }
}

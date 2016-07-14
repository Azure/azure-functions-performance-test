using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServerlessBenchmark.ServerlessPlatformControllers;

namespace ServerlessBenchmark.TriggerTests
{
    public abstract class BlobTriggerTest:StorageTriggerTest
    {
        protected readonly string SrcBlobContainer;
        protected readonly string DstBlobContainer;
        protected readonly string _functionName;

        protected abstract bool TestSetup();

        protected BlobTriggerTest(string functionName, string[] blobs, string sourceBlobContainer, string destinationBlobContainer):base(functionName, blobs)
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

        public BlobTriggerTest():base(null, null) { }

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

        protected override void UploadItems(IEnumerable<string> items)
        {
            UploadBlobs(items);
        }

        protected override bool VerifyTargetDestinationStorageCount(int expectedCount)
        {
            return VerifyBlobItemsExistInTargetDestination(expectedCount);
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

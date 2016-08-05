using System.Threading.Tasks;

namespace ServerlessBenchmark.ServerlessPlatformControllers
{
    /// <summary>
    /// Interface that serverless benchmark uses to communicate with the serverless platform.
    /// </summary>
    public interface ICloudPlatformController
    {
        CloudPlatformResponse PostMessage(CloudPlatformRequest request);
        CloudPlatformResponse PostMessages(CloudPlatformRequest request);
        Task<CloudPlatformResponse> PostMessagesAsync(CloudPlatformRequest request);
        CloudPlatformResponse GetMessage(CloudPlatformRequest request);
        CloudPlatformResponse GetMessages(CloudPlatformRequest request);
        CloudPlatformResponse DeleteMessages(CloudPlatformRequest request);
        Task<CloudPlatformResponse> EnqueueMessages(CloudPlatformRequest request);
        Task<CloudPlatformResponse> DequeueMessages(CloudPlatformRequest request);
        CloudPlatformResponse PostBlob(CloudPlatformRequest request);
        Task<CloudPlatformResponse> PostBlobAsync(CloudPlatformRequest request);
        Task<CloudPlatformResponse> PostBlobsAsync(CloudPlatformRequest request);
        CloudPlatformResponse ListBlobs(CloudPlatformRequest request);
        CloudPlatformResponse DeleteBlobs(CloudPlatformRequest request);
        CloudPlatformResponse GetFunctionName(string inputContainerName);
        CloudPlatformResponse GetInputOutputTriggers(string functionName);
    }
}

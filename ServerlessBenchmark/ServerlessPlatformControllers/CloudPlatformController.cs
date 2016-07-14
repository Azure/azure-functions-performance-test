using System.Threading.Tasks;

namespace ServerlessBenchmark.ServerlessPlatformControllers
{
    public interface ICloudPlatformController
    {
        CloudPlatformResponse PostMessage(CloudPlatformRequest request);
        CloudPlatformResponse PostMessages(CloudPlatformRequest request);
        CloudPlatformResponse GetMessage(CloudPlatformRequest request);
        CloudPlatformResponse GetMessages(CloudPlatformRequest request);
        CloudPlatformResponse DeleteMessages(CloudPlatformRequest request);

        CloudPlatformResponse PostBlob(CloudPlatformRequest request);
        Task<CloudPlatformResponse> PostBlobAsync(CloudPlatformRequest request);
        CloudPlatformResponse ListBlobs(CloudPlatformRequest request);
        CloudPlatformResponse DeleteBlobs(CloudPlatformRequest request);
        CloudPlatformResponse GetFunctionName(string inputContainerName);
        CloudPlatformResponse GetInputOutputTriggers(string functionName);
    }
}

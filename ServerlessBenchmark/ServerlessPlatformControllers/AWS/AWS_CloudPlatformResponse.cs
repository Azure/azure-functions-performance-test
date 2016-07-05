using System;
using Amazon.Runtime;

namespace ServerlessBenchmark.ServerlessPlatformControllers.AWS
{
    public class AwsCloudPlatformResponse:CloudPlatformResponse
    {
        public static AwsCloudPlatformResponse PopulateFrom(AmazonWebServiceResponse response)
        {
            if (response == null)
            {
                return null;
            }
            string insertionTime;
            DateTime insertionDateTime;
            response.ResponseMetadata.Metadata.TryGetValue(Constants.InsertionTime, out insertionTime);
            DateTime.TryParse(insertionTime, out insertionDateTime);
            var genericPlatformReponse = new AwsCloudPlatformResponse()
            {
                HttpStatusCode = response.HttpStatusCode,
                TimeStamp = insertionDateTime
            };
            return genericPlatformReponse;
        }
    }
}

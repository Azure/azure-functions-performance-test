using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;

namespace ServerlessBenchmark.TriggerTests
{
    public class AmazonS3TriggerTest:BlobTriggerTest
    {

        public AmazonS3TriggerTest(string functionName, IEnumerable<string> blobs, string sourceBlobContainer,
            string destinationBlobContainer)
            : base(functionName, blobs.ToArray(), sourceBlobContainer, destinationBlobContainer)
        {
            
        }

        public AmazonS3TriggerTest(string functionName, string[] blobs)
        {
            //todo get input,output container given the function name
            throw new NotImplementedException();
        }

        public AmazonS3TriggerTest(string functionName, string path, bool includeSubDirectories = false)
        {
            //todo get all files in this folder and use that for posting to aws
            throw new NotImplementedException();
        }

        protected override bool TestSetup()
        {
            return RemoveAllCLoudWatchLogs();
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AwsController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AwsGenericPerformanceResultsProvider(); }
        }

        private bool RemoveAllCLoudWatchLogs()
        {
            bool isEmpty;
            using (var cwClient = new AmazonCloudWatchLogsClient())
            {
                var logStreams = GetAllLogStreams(FunctionName, cwClient);
                Console.WriteLine("Deleting Log Streams");
                logStreams.ForEach(s => cwClient.DeleteLogStream(new DeleteLogStreamRequest("/aws/lambda/" + FunctionName, s.LogStreamName)));
                isEmpty = GetAllLogStreams(FunctionName, cwClient).Count == 0;
            }
            return isEmpty;
        }

        private List<LogStream> GetAllLogStreams(string functionName, AmazonCloudWatchLogsClient cwClient)
        {
            var logStreams = new List<LogStream>();
            DescribeLogStreamsResponse lResponse;
            string nextToken = null;
            do
            {
                lResponse =
                    cwClient.DescribeLogStreams(new DescribeLogStreamsRequest("/aws/lambda/" + functionName)
                    {
                        NextToken = nextToken
                    });
                logStreams.AddRange(lResponse.LogStreams);
            } while (!string.IsNullOrEmpty(nextToken = lResponse.NextToken));
            return logStreams;
        }
    }
}

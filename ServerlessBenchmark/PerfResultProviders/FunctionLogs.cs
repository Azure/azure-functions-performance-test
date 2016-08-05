using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServerlessBenchmark.PerfResultProviders
{
    internal class FunctionLogs
    {
        private static List<AzureFunctionLogs> _azurefunctionLogs; 
        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName, DateTime? startTime, DateTime? endTime, bool update = false, int expectedExecutionCount = 0)
        {
            Console.WriteLine("Getting Azure Function logs from Azure Storage Tables..");
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                var query = new TableQuery<AzureFunctionLogs>();
                int size = 0;
                do
                {
                    _azurefunctionLogs = table.ExecuteQuery(query).Where(log => !string.IsNullOrEmpty(log.FunctionName) &&
                        log.FunctionName.Equals(functionName, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(log.ContainerName)).ToList();
                    size = _azurefunctionLogs.Count();
                    Console.WriteLine("Log count - " + size);
                    if (expectedExecutionCount == 0)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                } while (size < expectedExecutionCount);
            }
            return _azurefunctionLogs;
        }

        public static List<AzureFunctionLogs> GetAzureFunctionLogs(DateTime? startTime, DateTime? endTime, bool update = false, int expectedExecutionCount = 0)
        {
            Console.WriteLine("Get Azure Function logs from Azure Storage Tables..");
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                var query = new TableQuery<AzureFunctionLogs>();
                int size = 0;
                do
                {
                    _azurefunctionLogs = table.ExecuteQuery(query).ToList();
                    size = _azurefunctionLogs.Count();
                    Console.WriteLine("Log count - " + size);
                    Thread.Sleep(1000);
                } while (size < expectedExecutionCount);
            }
            return _azurefunctionLogs;
        }

        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName, bool allLogs = false)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                return GetAzureFunctionLogs(null, null);
            }
            return GetAzureFunctionLogs(functionName, null, null);
        }

        public static bool RemoveAllCLoudWatchLogs(string functionName)
        {
            bool isEmpty;
            using (var cwClient = new AmazonCloudWatchLogsClient())
            {
                var logStreams = GetAllLogStreams(functionName, cwClient);
                Console.WriteLine("Deleting Log Streams");
                logStreams.ForEach(s => cwClient.DeleteLogStream(new DeleteLogStreamRequest("/aws/lambda/" + functionName, s.LogStreamName)));
                isEmpty = GetAllLogStreams(functionName, cwClient).Count == 0;
            }
            return isEmpty;
        }

        private static List<LogStream> GetAllLogStreams(string functionName, AmazonCloudWatchLogsClient cwClient)
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

        public class AzureFunctionLogs : TableEntity
        {
            public string FunctionName { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string ContainerName { get; set; }
            public Int32 TotalFail { get; set; }
            public Int32 TotalPass { get; set; }
        }
    }
}

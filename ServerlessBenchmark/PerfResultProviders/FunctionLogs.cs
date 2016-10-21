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
    public class FunctionLogs
    {
        public static ILogger _logger;
        private static CloudTable _azureFunctionLogTable;
        private static string _azureStorageConnectionString = null;
        private static CloudTable AzureFunctionLogTable
        {
            get
            {
                if (_azureFunctionLogTable == null)
                {
                    var connectionString = _azureStorageConnectionString ?? ConfigurationManager.AppSettings["AzureStorageConnectionString"];
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        try
                        {
                            var storageAccount = CloudStorageAccount.Parse(connectionString);
                            var tableClient = storageAccount.CreateCloudTableClient();
                            var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                            _azureFunctionLogTable = table;
                            return table;
                        }
                        catch (Exception)
                        {
                            _logger.LogWarning("Error in getting azure table");
                            throw;
                        }
                    }
                }
                return _azureFunctionLogTable;
            }
        }

        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName, DateTime? startTime, int expectedExecutionCount = 0, int waitForAllLogsTimeoutInMinutes = 2, bool includeIncomplete = false)
        {
            return GetAzureFunctionLogsInternal(functionName, startTime, expectedExecutionCount, waitForAllLogsTimeoutInMinutes, includeIncomplete);
        }

        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName)
        {
            return GetAzureFunctionLogs(functionName, null);
        }

        public static bool RemoveAllCLoudWatchLogs(string functionName)
        {
            bool isEmpty;
            using (var cwClient = new AmazonCloudWatchLogsClient())
            {
                var logStreams = GetAllLogStreams(functionName, cwClient);
                _logger.LogInfo("Deleting Log Streams");
                logStreams.ForEach(s => cwClient.DeleteLogStream(new DeleteLogStreamRequest("/aws/lambda/" + functionName, s.LogStreamName)));
                isEmpty = GetAllLogStreams(functionName, cwClient).Count == 0;
            }
            return isEmpty;
        }

        public static async Task<bool> PurgeAzureFunctionTableAsync(string storageConnectionString = null)
        {
            bool tablePurged = false;
            _azureStorageConnectionString = storageConnectionString;
            _logger.LogInfo("Purge Azure Function Table...");
            var table = AzureFunctionLogTable;
            var tableDeleted = await table.DeleteIfExistsAsync().ConfigureAwait(false);
            await Task.Delay(2000);
            await Utility.RetryHelperAsync(async () =>
            {
                tablePurged = await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            }, 
            _logger);
            return tablePurged;
        }

        private static List<AzureFunctionLogs> GetAzureFunctionLogsInternal(string functionName, DateTime? startTime, int expectedExecutionCount, int waitForAllLogsTimeoutInMinutes, bool includeIncomplete)
        {
            _logger.LogInfo("Getting Azure Function logs from Azure Storage Tables..");
            var connectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var _azurefunctionLogs = new List<AzureFunctionLogs>();

            if (!string.IsNullOrEmpty(connectionString))
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                int size = 0;
                var latestNewLog = DateTime.UtcNow;
                var lastSize = 0;

                do
                {
                    var query = table.CreateQuery<AzureFunctionLogs>().Where(x => x.PartitionKey == "R");

                    if (!string.IsNullOrEmpty(functionName))
                    {
                        query = query.Where(x => x.FunctionName.Equals(functionName, StringComparison.CurrentCultureIgnoreCase));
                    }

                    if (!includeIncomplete)
                    {
                        query = query.Where(x => x.RawStatus == "CompletedSuccess" || x.RawStatus == "CompletedFailure");
                    }

                    if (startTime.HasValue)
                    {
                        query = query.Where(x => x.StartTime >= startTime.Value);
                    }

                    _azurefunctionLogs = query.ToList();

                    size = _azurefunctionLogs.Count();

                    if (lastSize != size)
                    {
                        lastSize = size;
                        latestNewLog = DateTime.UtcNow;
                    }
                    else
                    {
                        var secondsSinceLastNewLog = (DateTime.UtcNow - latestNewLog).TotalSeconds;
                        var secondsStillToWait = 60 * waitForAllLogsTimeoutInMinutes - secondsSinceLastNewLog;
                        _logger.LogInfo(
                            "No new log for {0} seconds. Waiting another {1}s to finish.",
                            secondsSinceLastNewLog,
                            secondsStillToWait
                            );
                    }

                    if (expectedExecutionCount == 0)
                    {
                        break;
                    }

                    _logger.LogInfo("Log count {0} expected {1}", size, expectedExecutionCount);

                    if ((DateTime.UtcNow - latestNewLog).TotalMinutes >= waitForAllLogsTimeoutInMinutes)
                    {
                        _logger.LogInfo("Not all result logs have been found! No new logs appeared in last {0} minutes. Finishing wait to present results.", waitForAllLogsTimeoutInMinutes);
                        break;
                    }

                    Thread.Sleep(1000);
                } while (size < expectedExecutionCount);
            }

            return _azurefunctionLogs;
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
            public string RawStatus { get; set; }
            public Int32 TotalFail { get; set; }
            public Int32 TotalPass { get; set; }
        }
    }
}

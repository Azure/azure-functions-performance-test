using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.PerfResultProviders;
using System.Threading.Tasks;

namespace ServerlessBenchmark
{
    public class Utility
    {
        public static string GetCurrentLogsTableName()
        {
            // TODO: make this tool capable of doing a run that spans the end of a month
            return string.Format("{0}{1:yyyyMM}", Constants.AzureFunctionLogTableNamePrefix, DateTime.UtcNow);
        }

        /// <summary>
        /// Delete Azure Function Logs from an Azure storage account given the storage connection string.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="storageConnectionString"></param>
        /// <returns></returns>
        public static bool RemoveAzureFunctionLogs(string functionName, string storageConnectionString, ILogger logger)
        {
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                var operationContext = new OperationContext();
                var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var logs = FunctionLogs.GetAzureFunctionLogs(functionName);
                var table = tableClient.GetTableReference(GetCurrentLogsTableName());
                var partitions = logs.GroupBy(log => log.PartitionKey);
                var executionLogsCount = logs.Count(log => log.PartitionKey.Equals(Constants.AzureFunctionLogExecutionPartitionKey, 
                    StringComparison.CurrentCultureIgnoreCase));
                var count = 0;
                const int batchLimit = 100;
                IList<TableResult> deleteResults;

                foreach (var partition in partitions)
                {
                    var tb = new TableBatchOperation();
                    var logCount = partition.Count();
                    while (logCount > 0)
                    {
                        var logPool = partition.Skip(count);

                        //azure storage can only process 100 operations at any given time
                        var selectedLogs = logPool.Take(batchLimit).ToList();
                        selectedLogs.ForEach(log => tb.Add(TableOperation.Delete(log)));
                        try
                        {
                            deleteResults = table.ExecuteBatch(tb, null, operationContext);
                            tb.Clear();
                        }
                        catch (Exception e)
                        {
                            deleteResults = new List<TableResult>();
                            logger.LogException("Could not delete logs. {0}", e);
                        }

                        var deletedLogsCount = deleteResults.Count(result => result.HttpStatusCode == 204);
                        count += deletedLogsCount;
                        logger.LogInfo("Deleted {0}/{1} logs", count, executionLogsCount);

                        logCount = logCount - selectedLogs.Count;
                    }
                }
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retry asynchronous operations
        /// </summary>
        /// <param name="action"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        public static async Task RetryHelperAsync(Func<Task> action, ILogger logger, int retries = 5)
        {
            int attempt = 0;
            int delayMilliseconds = 5000;
            while (attempt < retries)
            {
                try
                {
                    await action().ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                    attempt++;
                    if (attempt < retries)
                    {
                        logger.LogWarning($"Attempt {attempt} failed");
                        logger.LogWarning($"Waiting {delayMilliseconds}ms until next attempt");
                        await Task.Delay(delayMilliseconds);
                        delayMilliseconds += delayMilliseconds;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}

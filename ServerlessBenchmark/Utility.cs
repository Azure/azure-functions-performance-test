using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.PerfResultProviders;

namespace ServerlessBenchmark
{
    public class Utility
    {
        /// <summary>
        /// Delete Azure Function Logs from an Azure storage account given the storage connection string.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="storageConnectionString"></param>
        /// <returns></returns>
        public static bool RemoveAzureFunctionLogs(string functionName, string storageConnectionString)
        {
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                var operationContext = new OperationContext();
                var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var logs = FunctionLogs.GetAzureFunctionLogs(functionName);
                var table = tableClient.GetTableReference(Constants.AzureFunctionLogTableName);
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
                            Console.WriteLine("--ERROR-- Could not delete logs. {0}", e);
                        }

                        var deletedLogsCount = deleteResults.Count(result => result.HttpStatusCode == 204);
                        count += deletedLogsCount;
                        Console.WriteLine("Deleted {0}/{1} logs", count, executionLogsCount);

                        logCount = logCount - selectedLogs.Count;
                    }
                }
                
                return true;
            }
            return false;
        }
    }
}

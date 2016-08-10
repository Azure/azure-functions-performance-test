using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using ServerlessBenchmark.PerfResultProviders;

namespace ServerlessBenchmark
{
    public class Utility
    {
        public static bool RemoveAzureFunctionLogs(string functionName, string storageConnectionString)
        {
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                var operationContext = new OperationContext();
                var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var logs = FunctionLogs.GetAzureFunctionLogs(functionName);
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                var partitions = logs.GroupBy(log => log.PartitionKey);
                var count = 0;
                const int batchLimit = 100;
                foreach (var partition in partitions)
                {
                    var tb = new TableBatchOperation();
                    var arr = partition.ToArray();
                    for (int i = 0; i < arr.Length; i++)
                    {
                        tb.Add(TableOperation.Delete(arr[i]));
                        count += 1;
                        if (i % (batchLimit - 1) == 0 && i > 0 || arr.Length < batchLimit)
                        {
                            try
                            {
                                table.ExecuteBatch(tb, operationContext: operationContext);
                                tb = new TableBatchOperation();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            if (operationContext.RequestResults.All(rs => rs.HttpStatusCode == 202))
                            {
                                Console.WriteLine("Deleted {0}/{1} logs", count, logs.Count);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}

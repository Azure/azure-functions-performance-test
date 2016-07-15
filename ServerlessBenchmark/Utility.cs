using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var logs = FunctionLogs.GetAzureFunctionLogs(null);
                var table = tableClient.GetTableReference("AzureFunctionsLogTable");
                logs.ForEach(entity => table.Execute(TableOperation.Delete(entity)));
                return true;
            }
            return false;
        }
    }
}

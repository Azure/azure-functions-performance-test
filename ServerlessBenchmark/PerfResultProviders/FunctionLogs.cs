using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServerlessBenchmark.PerfResultProviders
{
    internal class FunctionLogs
    {
        private static List<AzureFunctionLogs> _azurefunctionLogs; 
        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName, DateTime? startTime, DateTime? endTime, bool update = false, int expectedExecutionCount = 0)
        {
            if (_azurefunctionLogs == null || update)
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
                        _azurefunctionLogs = table.ExecuteQuery(query).Where(log => !string.IsNullOrEmpty(log.FunctionName) &&
                            log.FunctionName.Equals(functionName, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(log.ContainerName)).ToList();
                        size = _azurefunctionLogs.Count();
                        Console.WriteLine("Log count - " + size);
                        Thread.Sleep(1000);
                    } while (size < expectedExecutionCount);
                }
            }
            return _azurefunctionLogs;
        }

        public static List<AzureFunctionLogs> GetAzureFunctionLogs(string functionName, bool allLogs = false)
        {
            return GetAzureFunctionLogs(functionName, null, null);
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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.XPath;
using MiniCommandLineHelper;
using ServerlessBenchmark;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.TriggerTests;
using ServerlessBenchmark.TriggerTests.AWS;
using ServerlessBenchmark.TriggerTests.Azure;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace SampleUsages
{
    public class Program:CmdHelper
    {
        public new static void Main(string[] args)
        {
            var p = new Program();
            ((CmdHelper) p).Main(args);
        }

        [Command]
        [CommandLineAttribute("BlobTest <platform> <functionName> <blobPath> <srcBlobContainer> <targetBlobContainer> <loadProfile> [-eps:] [-repeat:] [-durationMinutes:]")]
        public void BlobTest(ServerlessPlatforms platform, string functionName, string blobPath, string srcBlobContainer, string targetBlobContainer, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            var blobs = Directory.GetFiles(blobPath);

            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && repeat)
            {
                if (durationMinutes <= 0)
                {
                    throw new ArgumentException("No parameter to specify how long to repeat this load. Indicate how long in minutes to repeat load.", "durationMinutes");
                }
                profile = new LinearLoad(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
            }
            else if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && !repeat)
            {
                profile = new LinearLoad(blobs.Count(), eps == 0 ? 1 : eps);
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }

            PerfTestResult functionResult;

            switch (platform)
            {
                case ServerlessPlatforms.Aws:
                    functionResult = AwsBlobTest(functionName, blobPath, srcBlobContainer, targetBlobContainer, profile, eps, repeat, durationMinutes);
                    break;
                default:
                    var azureFunctionsTest = new AzureBlobTriggerTest(functionName, blobs, srcBlobContainer, targetBlobContainer);
                    functionResult = azureFunctionsTest.RunAsync(profile).Result;
                    break;
            }

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;;
            Console.WriteLine(functionResult);
            Console.ForegroundColor = originalColor;
        }

        private PerfTestResult AwsBlobTest(string functionName, string blobPath, string srcBlobContainer, string targetBlobContainer, TriggerTestLoadProfile loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            var blobs = Directory.GetFiles(blobPath);
            var azureFunctionsTest = new AmazonS3TriggerTest(functionName, blobs, srcBlobContainer, targetBlobContainer);
            var perfResult = azureFunctionsTest.RunAsync(loadProfile).Result;
            return perfResult;
        }

        [Command]
        [CommandLineAttribute("QueueTest <platform> <functionName> <messages> <srcQueue> <targetQueue> <loadProfile> [-eps:] [-repeat:] [-durationMinutes:]")]
        public void QueueTest(ServerlessPlatforms platform, string functionName, string messages, string srcQueue, string targetQueue, string loadProfile, int eps = 0, bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            var queueMessages = File.ReadAllLines(messages);

            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && repeat)
            {
                if (durationMinutes <= 0)
                {
                    throw new ArgumentException("No parameter to specify how long to repeat this load. Indicate how long in minutes to repeat load.", "durationMinutes");
                }
                profile = new LinearLoad(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
            }
            else if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && !repeat)
            {
                profile = new LinearLoad(queueMessages.Count(), eps == 0 ? 1 : eps);
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }

            var azureFunctionsTest = new AzureQueueTriggerTest(functionName, queueMessages, srcQueue, targetQueue);
            var perfResult = azureFunctionsTest.RunAsync(profile).Result;

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green; ;
            Console.WriteLine(perfResult);
            Console.ForegroundColor = originalColor;
        }

        [Command]
        [CommandLineAttribute("HttpTest <platform> <functionName> <urlsFile> <loadProfile> [-eps:] [-repeat:] [-durationMinutes:]")]
        public void HttpTest(ServerlessPlatforms platform, string functionName, string urlsFile, string loadProfile, int eps = 0,
            bool repeat = false, int durationMinutes = 0)
        {
            TriggerTestLoadProfile profile;
            var urls = File.ReadAllLines(urlsFile);
            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && repeat)
            {
                if (durationMinutes <= 0)
                {
                    throw new ArgumentException("No parameter to specify how long to repeat this load. Indicate how long in minutes to repeat load.", "durationMinutes");
                }
                profile = new LinearLoad(TimeSpan.FromMinutes(durationMinutes), eps == 0 ? 1 : eps);
            }
            else if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase) && !repeat)
            {
                profile = new LinearLoad(urls.Count(), eps == 0 ? 1 : eps);
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }
            var azureFunctionsTest = new AzureHttpTriggerTest(functionName, urls);
            var perfResult = azureFunctionsTest.RunAsync(profile).Result;

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green; ;
            Console.WriteLine(perfResult);
            Console.ForegroundColor = originalColor;
        }

        public static void ShowCloudPlatformCompeteTable(IEnumerable<KeyValuePair<string, PerfTestResult>> results)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("===================== Benchmark and Comparison Tool ======================");
            //write headers
            var headers = new List<string>();
            headers.Add("Cloud Platform");
            headers.AddRange(results.First().Value.Keys.ToList());
            PrintLine();
            PrintRow(headers.ToArray());
            foreach (var cloudAndResults in results)
            {
                PrintLine();
                var cloudPlatformName = cloudAndResults.Key;
                headers.Clear();
                headers.Add(cloudPlatformName);
                headers.AddRange(cloudAndResults.Value.Values);
                PrintRow(headers.ToArray());
            }
            PrintLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Winner = {0}", "Azure Functions!");
            Console.Read();
        }

        public static PerfTestResult AmazonS3Test()
        {
            //set up
            var blobs = new List<string>();
            const string defaultSrcContainer = "hawk-original-images";
            const string defaultDstContainer = "hawk-original-images-thumbnail";
            for (int i = 0; i < 5; i++)
            {
                blobs.Add(@"C:\Users\hawfor\Pictures\original-image.jpg");
            }
            var s3Test = new AmazonS3TriggerTest("ImageResizerV2", blobs, defaultSrcContainer, defaultDstContainer);
            //var result = s3Test.Run();
            return null;
        }

        static int tableWidth = 120;

        static void PrintLine()
        {
            Console.WriteLine(new string('-', tableWidth));
        }

        static void PrintRow(params string[] columns)
        {
            int width = (tableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            Console.WriteLine(row);
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        }
    }
}

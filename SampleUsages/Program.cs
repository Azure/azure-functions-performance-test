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
        [CommandLineAttribute("BlobTest <functionName> <blobPath> <srcBlobContainer> <targetBlobContainer> <loadProfile> [<eps>]")]
        public void BlobTest(string functionName, string blobPath, string srcBlobContainer, string targetBlobContainer, string loadProfile, int eps = 1)
        {            
            var blobs = Directory.GetFiles(blobPath);
            TriggerTestLoadProfile profile;
            if (loadProfile.Equals("Linear", StringComparison.CurrentCultureIgnoreCase))
            {
                profile = new LinearLoad(TimeSpan.FromMinutes(1), blobs.Count());
            }
            else
            {
                throw new Exception(string.Format("{0} does not exist", loadProfile));
            }

            var azureFunctionsTest = new AzureBlobTriggerTest(functionName, blobs, srcBlobContainer, targetBlobContainer);
            var perfResult = azureFunctionsTest.Run(profile);

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;;
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

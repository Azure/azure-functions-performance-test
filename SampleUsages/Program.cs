using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.XPath;
using ServerlessBenchmark;
using ServerlessBenchmark.TriggerTests;

namespace SampleUsages
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var result = AmazonS3Test();
            var result5 = AzureBlobTest();
            return;
            var t1 = Task.Run(() => AmazonS3Test());
            var t2 = Task.Run(() => AzureBlobTest());
            var tasks = new Task[]{t1, t2};
            Task.WaitAll(tasks);
            var result = t1.Result;
            var result2 = t2.Result;

            var results = new List<KeyValuePair<string, PerfTestResult>>
            {
                new KeyValuePair<string, PerfTestResult>("Amazon", result),
                new KeyValuePair<string, PerfTestResult>("Azure", result2)
            };
            ShowCloudPlatformCompeteTable(results);
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
            var result = s3Test.Run();
            return result;
        }

        public static PerfTestResult AzureBlobTest()
        {
            var blobs = new List<string>();
            const string defaultSrcContainer = "input-image";
            const string defaultDstContainer = "output-images";
            for (int i = 0; i < 5; i++)
            {
                blobs.Add(@"C:\Users\hawfor\Pictures\original-image.jpg");
            }
            var azureFunctionsTest = new AzureBlobTriggerTest("ImageResizer", blobs, defaultSrcContainer, defaultDstContainer);
            var perfResult = azureFunctionsTest.Run();
            return perfResult;
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

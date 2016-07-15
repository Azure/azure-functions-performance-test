
using System.IO;
using ServerlessBenchmark.TriggerTests;

namespace ServerlessTestOrchestrator.TestScenarios
{
    internal class AzureBlobTestScenario : ITestScenario
    {
        private int _testObjectsCount;

        public void PrepareData(int testObjectsCount)
        {
            _testObjectsCount = testObjectsCount;
        }

        public IFunctionsTest GetBenchmarkTest(string functionName)
        {
            var defaultContainerName = "mycontainer";
            var defaultResultContainer = "myresultcontainer";
            var blobInputTempPath = "temp/";
            PrepareBlobInput(blobInputTempPath);
            var blobs = Directory.GetFiles(blobInputTempPath);
            return new AzureBlobTriggerTest(functionName, blobs, defaultContainerName, defaultResultContainer);
        }

        private void PrepareBlobInput(string blobInputTempPath)
        {
            for (var i = 0; i < _testObjectsCount; ++i)
            {
                var path = string.Format("{0}{1}.txt", blobInputTempPath, i);
                Directory.CreateDirectory(blobInputTempPath);
                var file = new System.IO.StreamWriter(path);
                // TODO: add content of the file from Blob-*-* directory
                file.WriteLine("150");
                file.Close();
            }
        }
    }
}

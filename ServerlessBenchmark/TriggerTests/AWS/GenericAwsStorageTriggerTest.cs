using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark.PerfResultProviders;
using ServerlessBenchmark.ServerlessPlatformControllers;
using ServerlessBenchmark.ServerlessPlatformControllers.AWS;
using ServerlessBenchmark.TriggerTests.BaseTriggers;

namespace ServerlessBenchmark.TriggerTests.AWS
{
    public abstract class GenericAwsStorageTriggerTest:StorageTriggerTest
    {
        private readonly StorageTriggerTest _storageTriggerTest;

        protected GenericAwsStorageTriggerTest(StorageTriggerTest storageTriggerTest) : base(storageTriggerTest.FunctionName, storageTriggerTest.SourceItems)
        {
            _storageTriggerTest = storageTriggerTest;
        }

        internal override string StorageType
        {
            get { return _storageTriggerTest.StorageType; }
        }

        internal override List<CloudPlatformResponse> CleanUpStorageResources()
        {
            return _storageTriggerTest.CleanUpStorageResources();
        }

        internal override bool VerifyTargetDestinationStorageCount(int expectedCount)
        {
            return _storageTriggerTest.VerifyTargetDestinationStorageCount(expectedCount);
        }

        internal override Task UploadItems(IEnumerable<string> items)
        {
            return _storageTriggerTest.UploadItems(items);
        }

        //AWS specific functions below

        protected override bool TestSetup()
        {
            return FunctionLogs.RemoveAllCLoudWatchLogs(FunctionName);
        }

        protected override ICloudPlatformController CloudPlatformController
        {
            get { return new AwsController(); }
        }

        protected override PerfResultProvider PerfmormanceResultProvider
        {
            get { return new AwsGenericPerformanceResultsProvider(); }
        }
    }
}

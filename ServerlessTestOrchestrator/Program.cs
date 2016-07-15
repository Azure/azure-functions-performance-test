using System;
using System.IO;
using System.Linq;
using MiniCommandLineHelper;
using SampleUsages;
using ServerlessBenchmark;
using ServerlessBenchmark.LoadProfiles;
using ServerlessBenchmark.TriggerTests;

namespace ServerlessTestOrchestrator
{
    public class Program : CmdHelper
    {
        public new static void Main(string[] args)
        {
            var p = new Program();
            ((CmdHelper)p).Main(args);
        }

        [Command]
        [CommandLineAttribute("TestScenario <functionName> <platform> <language> <trigger> <type> <loadType>")]
        public void RunScenario(string functionName, string platform, string language, string trigger, string type, string loadType)
        {
            TriggerType triggerType;
            TriggerType.TryParse(type, out triggerType);
            Language languageType;
            Language.TryParse(language, out languageType);
            Platorm platformType;
            PlatformID.TryParse(platform, out platformType);
            FunctionType functionType;
            FunctionType.TryParse(type, out functionType);

            var test = new TestDescription
            {
                FunctionName = functionName,
                Trigger = triggerType,
                Language = languageType,
                Platform = platformType,
                Type = functionType
            };

            var testTask = test.GenerateTestTask();
            var load = test.GenerateTestLoadProfile();
            var perfResult = testTask.RunAsync(load).Result;

            //print perf results
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green; ;
            Console.WriteLine(perfResult);
            Console.ForegroundColor = originalColor;

            Console.ReadKey();
        }
    }
}

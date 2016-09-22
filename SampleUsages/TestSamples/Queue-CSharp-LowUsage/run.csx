using System;

public static void Run(string input, out string output, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {input}");
    output = input;
}
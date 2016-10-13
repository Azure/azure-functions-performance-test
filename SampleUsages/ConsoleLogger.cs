using System;
using ServerlessBenchmark;

namespace SampleUsages
{
    class ConsoleLogger : ILogger
    {
        public void LogInfo(string info)
        {
            Console.WriteLine(info);
        }

        public void LogInfo(string format, params object[] args)
        {
            LogInfo(string.Format(format, args));
        }

        public void LogWarning(string warning)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"<WARNING> {warning}");
            Console.ForegroundColor = originalColor;
        }

        public void LogWarning(string format, params object[] args)
        {
            LogWarning(string.Format(format, args));
        }

        public void LogException(Exception ex)
        {
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            LogException("<EXCEPTION> {0}", ex);
            Console.ForegroundColor = fg;
        }

        public void LogException(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}

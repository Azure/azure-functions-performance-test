using System;

namespace ServerlessBenchmark
{
    public interface ILogger
    {
        void LogInfo(string info);
        void LogInfo(string format, params object[] args);
        void LogWarning(string warning);
        void LogWarning(string format, params object[] args);
        void LogException(Exception ex);
        void LogException(string format, params object[] args);
    }
}

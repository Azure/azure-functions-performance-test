using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerlessBenchmark;

namespace SampleUsages
{
    class FileLogger : ILogger, IDisposable
    {
        private StreamWriter logFile;

        public FileLogger(string filePath)
        {
            this.logFile = new StreamWriter(filePath);
            logFile.AutoFlush = true;
        }

        public void LogInfo(string info)
        {
            logFile.WriteLine($"<INFO> {info}");
        }

        public void LogInfo(string format, params object[] args)
        {
            LogInfo(string.Format(format, args));
        }

        public void LogWarning(string warning)
        {
            logFile.WriteLine($"<WARNING> {warning}");
        }

        public void LogWarning(string format, params object[] args)
        {
            LogWarning(string.Format(format, args));
        }

        public void LogException(Exception ex)
        {
            logFile.WriteLine($"<Exception> {ex}");
        }

        public void LogException(string format, params object[] args)
        {
            logFile.WriteLine(string.Format(format,args));
        }
        
        public void Dispose()
        {
            if (logFile != null)
            {
                logFile.Flush();
                logFile.Close();
            }
        }
    }
}

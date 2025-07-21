using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Logging.Core
{
    public abstract class BaseLogger : ILogger
    {
        protected BaseLogger(string name) { }

        public void LogCritical(string message)
        {
            Log(LoggingLevel.Critical, message);
        }

        public void LogDebug(string message)
        {
            Log(LoggingLevel.Debug, message);
        }

        public void LogError(string message)
        {
            Log(LoggingLevel.Error, message);
        }

        public void LogInformation(string message)
        {
            Log(LoggingLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            Log(LoggingLevel.Warn, message);
        }

        internal abstract void Log(LoggingLevel level, string message);
    }
}

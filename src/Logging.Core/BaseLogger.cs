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
        protected readonly string _serviceName;
        protected BaseLogger(string serviceName) {
            _serviceName = serviceName;
        }

        protected string ServiceName => _serviceName;

        protected abstract void Log(
            LogLevel level,
            string message,
            string? correlationId = null,
            object? context = null);

        public void LogDebug(string message, string? correlationId = null, object? context = null)
            => Log(LogLevel.Debug, message, correlationId, context);

        public void LogInformation(string message, string? correlationId = null, object? context = null)
            => Log(LogLevel.Info, message, correlationId, context);

        public void LogWarning(string message, string? correlationId = null, object? context = null)
            => Log(LogLevel.Warn, message, correlationId, context);

        public void LogError(string message, string? correlationId = null, object? context = null)
            => Log(LogLevel.Error, message, correlationId, context);

        public void LogCritical(string message, string? correlationId = null, object? context = null)
            => Log(LogLevel.Critical, message, correlationId, context);

        public abstract void Dispose();
    }
}

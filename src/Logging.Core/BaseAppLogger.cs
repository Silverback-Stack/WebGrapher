using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Logging.Core
{
    public abstract class BaseAppLogger : IAppLogger
    {
        protected readonly string _serviceName;
        protected BaseAppLogger(string serviceName) {
            _serviceName = serviceName;
        }

        protected string ServiceName => _serviceName;

        protected abstract void Log(
            AppLoggerLevel level,
            string message,
            string? correlationId = null,
            object? context = null);

        public abstract void Dispose();

        public void LogDebug(string message, string? correlationId = null, object? context = null)
            => Log(AppLoggerLevel.Debug, message, correlationId, context);

        public void LogInformation(string message, string? correlationId = null, object? context = null)
            => Log(AppLoggerLevel.Info, message, correlationId, context);

        public void LogWarning(string message, string? correlationId = null, object? context = null)
            => Log(AppLoggerLevel.Warn, message, correlationId, context);

        public void LogError(string message, string? correlationId = null, object? context = null)
            => Log(AppLoggerLevel.Error, message, correlationId, context);

        public void LogCritical(string message, string? correlationId = null, object? context = null)
            => Log(AppLoggerLevel.Critical, message, correlationId, context);
    }
}

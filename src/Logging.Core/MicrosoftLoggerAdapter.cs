using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Logging.Core
{
    public class MicrosoftLoggerAdapter : BaseLogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public MicrosoftLoggerAdapter(string name, Microsoft.Extensions.Logging.ILogger logger) : base(name)
        {
            _logger = logger;
        }

        protected override void Log(
            LogLevel level,
            string message,
            string? correlationId = null,
            object? context = null)
        {

            var logItem = new LogItem()
            {
                Service = ServiceName,
                Level = level,
                Message = message,
                CorrelationId = correlationId,
                Context = context
            };

            switch (level)
            {
                case LogLevel.Debug:
                    _logger.LogDebug(message);
                    break;

                case LogLevel.Info:
                    _logger.LogInformation(message);
                    break;

                case LogLevel.Warn:
                    _logger.LogWarning(message);
                    break;

                case LogLevel.Error:
                    _logger.LogError(message);
                    break;

                case LogLevel.Critical:
                    _logger.LogCritical(message);
                    break;

                default:
                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
            }
        }
        public override void Dispose()
        {
            if (_logger is IDisposable disposable) {
                Log(LogLevel.Info, $"Disposing: {typeof(MicrosoftLoggerAdapter).Name}.");
                disposable.Dispose();
            }       
        }

    }

}

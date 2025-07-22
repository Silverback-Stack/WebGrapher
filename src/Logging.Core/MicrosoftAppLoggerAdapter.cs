using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Logging.Core
{
    public class MicrosoftAppLoggerAdapter : BaseAppLogger
    {
        private readonly ILogger _logger;

        public MicrosoftAppLoggerAdapter(string name, ILogger logger) : base(name)
        {
            _logger = logger;
        }

        public override void Dispose()
        {
            Log(AppLoggerLevel.Info, $"Disposing: {typeof(MicrosoftAppLoggerAdapter).Name}.");
            if (_logger is IDisposable disposable)
                disposable.Dispose();
        }

        protected override void Log(
            AppLoggerLevel level,
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
                case AppLoggerLevel.Debug:
                    _logger.LogDebug(message);
                    break;

                case AppLoggerLevel.Info:
                    _logger.LogInformation(message);
                    break;

                case AppLoggerLevel.Warn:
                    _logger.LogWarning(message);
                    break;

                case AppLoggerLevel.Error:
                    _logger.LogError(message);
                    break;

                case AppLoggerLevel.Critical:
                    _logger.LogCritical(message);
                    break;

                default:
                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
            }
        }
    }

}

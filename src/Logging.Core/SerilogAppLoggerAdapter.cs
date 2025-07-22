using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Logging.Core
{
    public class SerilogAppLoggerAdapter : BaseAppLogger
    {
        private readonly ILogger _logger;

        public SerilogAppLoggerAdapter(string name, ILogger logger) : base(name)
        {
            _logger = logger;
        }

        public override void Dispose()
        {
            Log(AppLoggerLevel.Info, $"Disposing: {typeof(SerilogAppLoggerAdapter).Name}.");
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
                    _logger.Debug(message);
                    break;

                case AppLoggerLevel.Info:
                    _logger.Information(message);
                    break;

                case AppLoggerLevel.Warn:
                    _logger.Warning(message);
                    break;

                case AppLoggerLevel.Error:
                    _logger.Error(message);
                    break;

                case AppLoggerLevel.Critical:
                    _logger.Fatal(message);
                    break;

                default:
                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
            }
        }
    }
}

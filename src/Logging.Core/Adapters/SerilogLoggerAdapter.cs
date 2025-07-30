using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Logging.Core.Adapters
{
    public class SerilogLoggerAdapter : BaseLogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogLoggerAdapter(string name, Serilog.ILogger logger) : base(name)
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
                    _logger.Debug(message);
                    break;

                case LogLevel.Info:
                    _logger.Information(message);
                    break;

                case LogLevel.Warn:
                    _logger.Warning(message);
                    break;

                case LogLevel.Error:
                    _logger.Error(message);
                    break;

                case LogLevel.Critical:
                    _logger.Fatal(message);
                    break;

                default:
                    throw new NotSupportedException($"Logging level '{level}' is not supported.");
            }
        }
        public override void Dispose()
        {
            if (_logger is IDisposable disposable)
            {
                Log(LogLevel.Info, $"Disposing: {typeof(SerilogLoggerAdapter).Name}.");
                disposable.Dispose();
            }
        }
    }
}

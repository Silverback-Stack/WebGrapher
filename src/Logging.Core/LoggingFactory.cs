using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;

namespace Logger.Core
{
    public static class LoggingFactory
    {
        public static ILoggerFactory CreateLogger(IConfiguration configuration, string serviceName)
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("EnvironmentName", configuration.GetEnvironmentName())
                // File sink for dynamic filenames
                .WriteTo.File(
                    path: $"logs/{serviceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            // Assign to global logger
            Log.Logger = logger;

            return new SerilogLoggerFactory(logger);
        }
    }
}

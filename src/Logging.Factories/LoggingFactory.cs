using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Logging.Factories
{
    public static class LoggingFactory
    {
        public static ILoggerFactory CreateLogger(
            IConfiguration configuration,
            string serviceName,
            string environmentName)
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("EnvironmentName", environmentName)
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

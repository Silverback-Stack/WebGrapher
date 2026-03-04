using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Logging.Factories
{
    public static class LoggingFactory
    {
        public static ILoggerFactory CreateLoggerFactory(
            IConfiguration configuration,
            string serviceName,
            string environmentName)
        {
            configuration = ReplaceTokenInFilePath(
                configuration, "{ServiceName}", serviceName);

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("EnvironmentName", environmentName)
                .CreateLogger();

            // Assign to global logger
            Log.Logger = logger;

            return new SerilogLoggerFactory(logger);
        }


        /// <summary>
        /// Replaces a placeholder token in the Serilog File sink path configuration.
        /// </summary>
        private static IConfiguration ReplaceTokenInFilePath(
            IConfiguration configuration,
            string token,
            string value)
        {
            // Locate the File sink inside Serilog:WriteTo
            var fileSink = configuration.GetSection("Serilog:WriteTo")
                .GetChildren()
                .FirstOrDefault(s =>
                    string.Equals(s["Name"], "File", StringComparison.OrdinalIgnoreCase));

            if (fileSink is null)
                return configuration;

            // Build the configuration key for Args:path
            var key = $"{fileSink.Path}:Args:path";

            // Read the configured path template
            var pathTemplate = configuration[key];

            if (string.IsNullOrWhiteSpace(pathTemplate))
                return configuration;

            // Replace the token and override the value in-memory
            return new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddInMemoryCollection(new Dictionary<string, string?> { 
                    [key] = pathTemplate.Replace(token, value)
                })
                .Build();
        }
    }
}

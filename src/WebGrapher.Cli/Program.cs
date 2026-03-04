using App.Settings;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace WebGrapher.Cli
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Enable support for legacy encodings used by some web pages and older systems:
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


            // Create a host builder and retrieve the application environment
            var builder = Host.CreateApplicationBuilder(args);
            var environment = builder.Environment;
            var serviceName = typeof(Program).Namespace!;


            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging");


            // Create logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings,
                serviceName,
                environment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("WebGrapher.Cli is starting using {EnvironmentName} configuration.",
                    environment.EnvironmentName);
            logger.LogDebug("DEBUG output is enabled.");


            // Global exception handler
            try
            {
                var webGrapherApp = new WebGrapherApp(environment);
                await webGrapherApp.InitializeAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "The application was terminated due to a fatal error.");

                Console.ReadKey();
            }
        }

    }
}
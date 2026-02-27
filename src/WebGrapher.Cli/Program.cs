// See https://aka.ms/new-console-template for more information
using App.Settings;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using WebGrapher.Cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Enable support for legacy encodings used by some web pages and older systems:
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Create a host builder to retrieve the current application environment
        var builder = Host.CreateApplicationBuilder(args);
        var environment = builder.Environment;

        // Load appsettings.json and environment overrides
        var cliAppSettings = ConfigurationLoader.LoadConfiguration(
            environment.EnvironmentName, "Logging");

        // Create logger
        ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
            cliAppSettings, "WebGrapher.Cli", environment.EnvironmentName);
        var logger = loggerFactory.CreateLogger<Program>();

        // Global exception handler
        try
        {
            logger.LogInformation("WebGrapher.Cli is starting using {EnvironmentName} configuration.",
                environment.EnvironmentName);

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
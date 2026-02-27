using Events.Core.Bus;
using Graphing.Core;
using Logging.Factories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Graphing.WebApi;
using Auth.WebApi;
using App.Settings;
using Graphing.Factories;

namespace WebGrapher.Cli.Services
{
    internal class GraphingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(
            IEventBus eventBus,
            IHostEnvironment environment)
        {
            // Load appsettings.json and environment overrides
            var graphingAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Graphing");
            var authAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Auth");

            // Bind configuration overrides onto settings objects
            var graphingConfig = graphingAppSettings.BindSection<GraphingConfig>("Graphing");
            var graphingWebApiConfig = graphingAppSettings.BindSection<GraphingWebApiConfig>("GraphingWebApi");
            var authConfig = authAppSettings.BindSection<AuthConfig>("Auth");


            // Setup Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                graphingAppSettings, 
                graphingConfig.Settings.ServiceName, 
                environment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                graphingConfig.Settings.ServiceName,
                environment.EnvironmentName,
                graphingWebApiConfig.Host,
                graphingWebApiConfig.Swagger.RoutePrefix);

            // Create Graphing Service
            var graphingService = GraphingFactory.Create(logger, eventBus, graphingConfig);
            await graphingService.StartAsync();

            //Start WEB.API:
            _host = await StartWebApiServerAsync(graphingService, graphingWebApiConfig, authConfig);
        }

        private async static Task<IHost> StartWebApiServerAsync(
            IPageGrapher graphingService, 
            GraphingWebApiConfig graphingWebApiConfig,
            AuthConfig authConfig)
        {
            var builder = WebApplication.CreateBuilder();

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Graphing API
            builder.Services.AddGraphingWebApi(graphingService, graphingWebApiConfig, authConfig);

            var app = builder.Build();

            app.UseGraphingWebApi(graphingWebApiConfig);

            app.Urls.Add(graphingWebApiConfig.Host);

            await app.StartAsync();

            return app;
        }

        public static async Task StopWebApiServerAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                await _host.WaitForShutdownAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
        }
    }
}

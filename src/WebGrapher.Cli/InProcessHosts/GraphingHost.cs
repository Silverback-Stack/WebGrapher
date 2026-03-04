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

namespace WebGrapher.Cli.InProcessHosts
{
    public class GraphingHost
    {
        private readonly IEventBus _eventBus;
        private readonly IHostEnvironment _hostEnvironment;
        private static IHost? _host;

        public GraphingHost(
            IEventBus eventBus,
            IHostEnvironment hostEnvironment)
        {
            _eventBus = eventBus;
            _hostEnvironment = hostEnvironment;
        }

        public async Task StartAsync()
        {
            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                _hostEnvironment.EnvironmentName, "Logging", "Graphing", "Auth");

            // Bind configuration overrides onto settings objects
            var graphingConfig = appSettings.BindSection<GraphingConfig>("Graphing");
            var graphingWebApiConfig = appSettings.BindSection<GraphingWebApiConfig>("GraphingWebApi");
            var authConfig = appSettings.BindSection<AuthConfig>("Auth");


            // Setup Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                graphingConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                graphingConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName,
                graphingWebApiConfig.Host,
                graphingWebApiConfig.Swagger.RoutePrefix);

            // Create Graphing Service
            var graphingService = GraphingFactory.Create(logger, _eventBus, graphingConfig);
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

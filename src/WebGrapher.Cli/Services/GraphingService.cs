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

namespace WebGrapher.Cli.Services
{
    internal class GraphingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            // Load configurations
            var configGraphing = ConfigurationLoader.LoadConfiguration("Graphing");
            var graphingSettings = configGraphing.BindSection<GraphingSettings>("Graphing");
            var graphingWebApiSettings = configGraphing.BindSection<GraphingWebApiSettings>("GraphingWebApi");

            var configAuth = ConfigurationLoader.LoadConfiguration("Auth");
            var authSettings = configAuth.BindSection<AuthSettings>("Auth");


            // Setup Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configGraphing, graphingSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                graphingSettings.ServiceName,
                configGraphing.GetEnvironmentName(),
                graphingWebApiSettings.Host,
                graphingWebApiSettings.Swagger.RoutePrefix);

            // Create Graphing Service
            var graphingService = GraphingFactory.Create(logger, eventBus, graphingSettings);
            await graphingService.StartAsync();

            //Start WEB.API:
            _host = await StartWebApiServerAsync(graphingService, graphingWebApiSettings, authSettings);
        }

        private async static Task<IHost> StartWebApiServerAsync(
            IPageGrapher graphingService, 
            GraphingWebApiSettings graphingWebApiSettings,
            AuthSettings authSettings)
        {
            var builder = WebApplication.CreateBuilder();

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Graphing API
            builder.Services.AddGraphingWebApi(graphingService, graphingWebApiSettings, authSettings);

            var app = builder.Build();

            app.UseGraphingWebApi(graphingWebApiSettings);

            app.Urls.Add(graphingWebApiSettings.Host);

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

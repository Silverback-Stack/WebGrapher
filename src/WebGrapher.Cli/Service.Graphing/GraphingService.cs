using Events.Core.Bus;
using Graphing.Core;
using Logger.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Settings.Core;
using Graphing.WebApi;

namespace WebGrapher.Cli.Service.Graphing
{
    internal class GraphingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            // Load Configuration
            var configGraphing = ConfigurationLoader.LoadConfiguration("Service.Graphing");
            var webApiSettings = configGraphing.BindSection<WebApiSettings>("WebApi");
            var graphingSettings = configGraphing.BindSection<GraphingSettings>("Graphing");


            // Setup Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configGraphing, graphingSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                graphingSettings.ServiceName,
                configGraphing.GetEnvironmentName(),
                webApiSettings.Host,
                webApiSettings.SwaggerRoutePrefix);

            // Create Graphing Service
            var graphingService = GraphingFactory.Create(logger, eventBus, graphingSettings);
            await graphingService.StartAsync();

            //Start WEB.API:
            _host = await StartWebApiServerAsync(graphingService, webApiSettings);
        }

        private async static Task<IHost> StartWebApiServerAsync(IPageGrapher graphingService, WebApiSettings webApiSettings)
        {
            var builder = WebApplication.CreateBuilder();

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Graphing API
            builder.Services.AddGraphingWebApi(graphingService);

            var app = builder.Build();

            app.UseGraphingWebApi(webApiSettings);

            app.Urls.Add(webApiSettings.Host);

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

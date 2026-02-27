using App.Settings;
using Auth.WebApi;
using Events.Core.Bus;
using Events.Factories;
using Graphing.Core;
using Graphing.Factories;
using Graphing.WebApi;
using Logging.Factories;
using Serilog;

namespace Graphing.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var environment = builder.Environment;

            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Events");
            var graphingAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Graphing");
            var authAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Auth");

            var eventBusConfig = eventsAppSettings.BindSection<EventsConfig>("Events");
            var graphingConfig = graphingAppSettings.BindSection<GraphingConfig>("Graphing");
            var graphingWebApiConfig = graphingAppSettings.BindSection<GraphingWebApiConfig>("GraphingWebApi");
            var authConfig = authAppSettings.BindSection<AuthConfig>("Auth");


            // Create Logger
            LoggingFactory.CreateLogger(
                graphingAppSettings, 
                graphingConfig.Settings.ServiceName,
                environment.EnvironmentName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);


            // Register Event Bus in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventsFactory.CreateEventBus(logger, eventBusConfig);
            });

            // Register Settings in DI
            builder.Services.AddSingleton(graphingWebApiConfig);
            builder.Services.AddSingleton(authConfig);

            // Register Graphing Service in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageGrapher>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Graphing Service
                var graphingService = GraphingFactory.Create(
                    logger,
                    eventBus,
                    graphingConfig);

                return graphingService;
            });


            builder.Services.AddHostedService<WebApiHostedService>();

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{graphingConfig.Settings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
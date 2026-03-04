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
            var hostEnvironment = builder.Environment;

            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                hostEnvironment.EnvironmentName, "Logging", "Events", "Graphing", "Auth");

            var eventBusConfig = appSettings.BindSection<EventsConfig>("Events");
            var graphingConfig = appSettings.BindSection<GraphingConfig>("Graphing");
            var graphingWebApiConfig = appSettings.BindSection<GraphingWebApiConfig>("GraphingWebApi");
            var authConfig = appSettings.BindSection<AuthConfig>("Auth");


            // Create Logger
            LoggingFactory.CreateLoggerFactory(
                appSettings, 
                graphingConfig.Settings.ServiceName,
                hostEnvironment.EnvironmentName);
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
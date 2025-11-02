using Auth.WebApi;
using Events.Core.Bus;
using Graphing.Core;
using Graphing.WebApi;
using Logger.Core;
using Serilog;
using Settings.Core;

namespace Graphing.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load configurations
            var configEvents = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventsSettings = configEvents.BindSection<EventBusSettings>("EventBus");

            var configGraphing = ConfigurationLoader.LoadConfiguration("Service.Graphing");
            var graphingSettings = configGraphing.BindSection<GraphingSettings>("Graphing");
            var graphingWebApiSettings = configGraphing.BindSection<GraphingWebApiSettings>("GraphingWebApi");

            var configAuth = ConfigurationLoader.LoadConfiguration("Shared.Auth");
            var authSettings = configAuth.BindSection<AuthSettings>("Auth");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configGraphing, graphingSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Register Event Bus in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });

            // Register Settings in DI
            builder.Services.AddSingleton(graphingWebApiSettings);
            builder.Services.AddSingleton(authSettings);

            // Register Graphing Service in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageGrapher>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Graphing Service
                var graphingService = GraphingFactory.Create(
                    logger,
                    eventBus,
                    graphingSettings);

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
                Log.Fatal(ex, $"{graphingSettings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
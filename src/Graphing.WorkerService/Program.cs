using Events.Core.Bus;
using Graphing.Core;
using Logger.Core;
using Serilog;
using Settings.Core;
using Graphing.WebApi;

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
            var webApiSettings = configGraphing.BindSection<WebApiSettings>("WebApi");
            var graphingSettings = configGraphing.BindSection<GraphingSettings>("Graphing");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configGraphing, graphingSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });

            // Register WebApi Settings as a singleton in DI
            builder.Services.AddSingleton(webApiSettings);

            // Register Graphing Service as a singleton in DI
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
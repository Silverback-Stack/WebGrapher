using App.Settings;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebGrapher.Cli
{
    internal class EventBusComposition
    {
        public static IEventBus Create(IHostEnvironment hostEnvironment)
        {
            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                hostEnvironment.EnvironmentName, "Logging", "Events");

            // Bind configuration overrides onto settings objects
            var eventsConfig = appSettings.BindSection<EventsConfig>("Events");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                eventsConfig.Settings.ServiceName, 
                hostEnvironment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IEventBus>();
       
            logger.LogInformation("{ServiceName} event bus is initializing using {EnvironmentName} configuration.",
                eventsConfig.Settings.ServiceName, hostEnvironment.EnvironmentName);


            // Create Event Bus
            var eventBus = EventsFactory.CreateEventBus(
                logger, eventsConfig);

            return eventBus;
        }
    }
}

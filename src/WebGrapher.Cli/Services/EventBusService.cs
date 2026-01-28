using App.Settings;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Microsoft.Extensions.Logging;

namespace WebGrapher.Cli.Services
{
    internal class EventBusService
    {
        public static Task<IEventBus> CreateAsync()
        {
            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(path: "Events");

            // Bind configuration overrides onto settings objects
            var eventsConfig = eventsAppSettings.BindSection<EventsConfig>("Events");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                eventsAppSettings, eventsConfig.Settings.ServiceName);
            var logger = loggerFactory.CreateLogger<IEventBus>();


            // Create Event Bus
            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                eventsConfig.Settings.ServiceName, eventsAppSettings.GetEnvironmentName());

            var eventBus = EventsFactory.CreateEventBus(logger, eventsConfig);

            return Task.FromResult(eventBus);
        }
    }
}

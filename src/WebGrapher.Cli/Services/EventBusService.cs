using App.Settings;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebGrapher.Cli.Services
{
    internal class EventBusService
    {
        public static Task<IEventBus> InitializeAsync(IHostEnvironment environment)
        {
            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Events");

            // Bind configuration overrides onto settings objects
            var eventsConfig = eventsAppSettings.BindSection<EventsConfig>("Events");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                eventsAppSettings, 
                eventsConfig.Settings.ServiceName, 
                environment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IEventBus>();


            // Create Event Bus
            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                eventsConfig.Settings.ServiceName, environment.EnvironmentName);

            var eventBus = EventsFactory.CreateEventBus(
                logger, eventsConfig);

            return Task.FromResult(eventBus);
        }
    }
}

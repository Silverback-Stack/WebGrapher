using Events.Core.Bus;
using Logger.Core;
using Microsoft.Extensions.Logging;
using Settings.Core;

namespace WebGrapher.Cli.Service.Events
{
    internal class EventBusService
    {
        public static Task<IEventBus> CreateAsync()
        {
            // Load Configuration
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventBusSettings = configuration.BindSection<EventBusSettings>("EventBus");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configuration, eventBusSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IEventBus>();


            // Create Event Bus
            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                eventBusSettings.ServiceName, configuration.GetEnvironmentName());

            var eventBus = EventBusFactory.Create(logger, eventBusSettings);

            return Task.FromResult(eventBus);
        }
    }
}

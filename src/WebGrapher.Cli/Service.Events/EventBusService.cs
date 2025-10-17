using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;

namespace WebGrapher.Cli.Service.Events
{
    internal class EventBusService
    {
        public async static Task<IEventBus> CreateAsync()
        {
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Events");

            //Bind to strongly typed objects
            var eventBusSettings = configuration.BindSection<EventBusSettings>("EventBus");

            // Setup Serilog Logging
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    path: $"logs/{eventBusSettings.ServiceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IEventBus>();

            var eventBus = EventBusFactory.CreateEventBus(logger, eventBusSettings);

            logger.LogInformation("{ServiceName} service started.",
                eventBusSettings.ServiceName);

            return eventBus;
        }
    }
}

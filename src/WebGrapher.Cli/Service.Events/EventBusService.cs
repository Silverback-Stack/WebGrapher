using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Events
{
    internal class EventBusService
    {
        public async static Task<IEventBus> CreateAsync(EventBusSettings eventBusSettings)
        {
            //configure logging:
            var serviceName = eventBusSettings.ServiceName;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IEventBus>();

            var eventBus = EventBusFactory.CreateEventBus(logger, eventBusSettings);

            logger.LogInformation("Event bus service started.");

            return eventBus;
        }
    }
}

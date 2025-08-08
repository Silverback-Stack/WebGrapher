using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Events
{
    internal class EventBusService
    {
        public async static Task<IEventBus> StartAsync()
        {
            var serviceName = typeof(EventBusService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IEventBus>();

            var eventBus = EventBusFactory.CreateEventBus(logger);
            await eventBus.StartAsync();

            logger.LogInformation("Event bus service started.");

            return eventBus;
        }
    }
}

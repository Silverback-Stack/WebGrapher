using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Events
{
    internal class EventBusService
    {
        public static IEventBus Start()
        {
            var serviceName = typeof(EventBusService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                //.WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IEventBus>();


            var eventBus = EventBusFactory.CreateEventBus(logger);
            eventBus.StartAsync();
            return eventBus;
        }
    }
}

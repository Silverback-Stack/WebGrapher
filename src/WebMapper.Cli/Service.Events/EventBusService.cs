using System;
using Events.Core.Bus;
using Events.Core.Events;
using Events.Core.RateLimiters;
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
            //configure logging:
            var serviceName = typeof(EventBusService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IEventBus>();


            //configure event bus rate limits:
            var concurrencyLimits = new Dictionary<Type, int>
            {
                { typeof(CrawlPageEvent), 10 },
                { typeof(ScrapePageEvent), 10 },
                { typeof(ScrapePageFailedEvent), 10 },
                { typeof(NormalisePageEvent), 10 },
                { typeof(GraphPageEvent), 10 },
                { typeof(GraphNodeAddedEvent), 10 }
            };

            var eventBus = EventBusFactory.CreateEventBus(logger, concurrencyLimits);
            await eventBus.StartAsync();

            logger.LogInformation("Event bus service started.");

            return eventBus;
        }
    }
}

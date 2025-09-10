using System;
using Events.Core.Bus;
using Events.Core.Events;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Events
{
    internal class EventBusService
    {
        public async static Task<IEventBus> StartAsync(EventBusSettings eventBusSettings)
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


            //configure event bus rate limits:
            var concurrencyLimits = new Dictionary<Type, int>
            {
                { typeof(CrawlPageEvent), eventBusSettings.RateLimiter.MaxCrawlPageEvents },
                { typeof(ScrapePageEvent), eventBusSettings.RateLimiter.MaxScrapePageEvents },
                { typeof(NormalisePageEvent), eventBusSettings.RateLimiter.MaxNormalisePageEvents },
                { typeof(GraphPageEvent), eventBusSettings.RateLimiter.MaxGraphPageEvents },
                { typeof(GraphNodeAddedEvent), eventBusSettings.RateLimiter.MaxGraphNodeAddedEvents }
            };

            var eventBus = EventBusFactory.CreateEventBus(logger, concurrencyLimits);
            await eventBus.StartAsync();

            logger.LogInformation("Event bus service started.");

            return eventBus;
        }
    }
}

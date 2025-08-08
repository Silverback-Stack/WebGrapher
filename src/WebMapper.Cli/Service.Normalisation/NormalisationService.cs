using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Normalisation
{
    internal class NormalisationService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(NormalisationService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IPageNormaliser>();

            var cache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InStorage,
                logger);

            NormalisationFactory.CreateNormaliser(logger, cache, eventBus);

            logger.LogInformation("Normalisation service started.");
        }
    }
}

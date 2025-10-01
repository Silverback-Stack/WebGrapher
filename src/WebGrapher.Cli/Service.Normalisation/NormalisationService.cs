using System;
using Caching.Core;
using Settings.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Normalisation
{
    internal class NormalisationService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Normalisation");

            //bind appsettings overrides to default settings objects
            var normalisationSettings = configuration.BindSection<NormalisationSettings>("Normalisation");


            //Setup Logging
            var logFilePath = $"logs/{normalisationSettings.ServiceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageNormaliser>();


            //Create Blob Cache
            var blobCacheSettings = new CacheSettings();
            configuration.GetSection("BlobCache").Bind(blobCacheSettings);
            var cache = CacheFactory.CreateCache(
                normalisationSettings.ServiceName,
                logger,
                blobCacheSettings);


            //Create Normalisation
            NormalisationFactory.CreateNormaliser(logger, cache, eventBus, normalisationSettings);

            logger.LogInformation("Normalisation service started.");
        }
    }
}

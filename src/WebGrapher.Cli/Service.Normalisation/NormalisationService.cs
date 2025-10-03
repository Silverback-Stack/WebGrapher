using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;

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

            // Setup Serilog Logging
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    path: $"logs/{normalisationSettings.ServiceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
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

            logger.LogInformation("{ServiceName} service started.",
                normalisationSettings.ServiceName);
        }
    }
}

using App.Settings;
using Caching.Factories;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Crawler.Factories;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Requests.Factories;
using Serilog;

namespace Crawler.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var hostEnvironment = builder.Environment;

            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                hostEnvironment.EnvironmentName, "Logging", "Events", "Crawler");

            // Bind configuration overrides onto settings objects
            var eventBusConfig = appSettings.BindSection<EventsConfig>("Events");
            var crawlerConfig = appSettings.BindSection<CrawlerConfig>("Crawler");
            var metaCacheConfig = appSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = appSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = appSettings.BindSection<RequestsConfig>("Requests");
            var policyCacheConfig = appSettings.BindSection<CacheConfig>("PolicyCache");


            // Create Logger
            LoggingFactory.CreateLoggerFactory(
                appSettings, 
                crawlerConfig.Settings.ServiceName, 
                hostEnvironment.EnvironmentName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventsFactory.CreateEventBus(logger, eventBusConfig);
            });


            // Register Crawler Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageCrawler>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    crawlerConfig.Settings.ServiceName,
                    logger,
                    metaCacheConfig);

                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    crawlerConfig.Settings.ServiceName,
                    logger,
                    blobCacheConfig);

                // Create Request Sender
                var requestSender = RequestsFactory.Create(
                    logger, 
                    metaCache, 
                    blobCache,
                    requestsConfig);

                // Create Policy Cache for Site Policy Resolver
                var policyCache = CacheFactory.Create(
                    crawlerConfig.Settings.ServiceName,
                    logger,
                    policyCacheConfig);

                // Create Site Policy Resolver
                var sitePolicyResolver = new SitePolicyResolver(
                    logger, 
                    policyCache, 
                    requestSender,
                    crawlerConfig.Settings);

                // Create Crawler Service
                var crawlerService = CrawlerFactory.Create(
                    logger, 
                    eventBus, 
                    sitePolicyResolver,
                    crawlerConfig);

                return crawlerService;
            });


            // Add Worker background service
            builder.Services.AddHostedService<Worker>();

            // Build and run
            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{crawlerConfig.Settings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
        }
    }
}
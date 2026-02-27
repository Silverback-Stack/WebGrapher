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
            var environment = builder.Environment;

            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Events");
            var crawlerAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Crawler");

            // Bind configuration overrides onto settings objects
            var eventBusConfig = eventsAppSettings.BindSection<EventsConfig>("Events");
            var crawlerConfig = crawlerAppSettings.BindSection<CrawlerConfig>("Crawler");
            var metaCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = crawlerAppSettings.BindSection<RequestsConfig>("Requests");
            var policyCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("PolicyCache");


            // Create Logger
            LoggingFactory.CreateLogger(
                crawlerAppSettings, 
                crawlerConfig.Settings.ServiceName, 
                environment.EnvironmentName);
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
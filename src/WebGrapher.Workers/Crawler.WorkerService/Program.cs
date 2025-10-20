using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Logger.Core;
using Requests.Core;
using Serilog;
using Settings.Core;

namespace Crawler.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load configurations
            var configEvents = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventsSettings = configEvents.BindSection<EventBusSettings>("EventBus");

            var configCrawler = ConfigurationLoader.LoadConfiguration("Service.Crawler");
            var crawlerSettings = configCrawler.BindSection<CrawlerSettings>("Crawler");
            var metaCacheSettings = configCrawler.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configCrawler.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configCrawler.BindSection<RequestSenderSettings>("RequestSender");
            var policyCacheSettings = configCrawler.BindSection<CacheSettings>("PolicyCache");


            // Setup Logging
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configCrawler, crawlerSettings.ServiceName);

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Create Event Bus
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.CreateEventBus(logger, eventsSettings);
            });


            // Create Crawler Service
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageCrawler>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                var metaCache = CacheFactory.CreateCache(
                    crawlerSettings.ServiceName,
                    logger,
                    metaCacheSettings);

                var blobCache = CacheFactory.CreateCache(
                    crawlerSettings.ServiceName,
                    logger,
                    blobCacheSettings);

                var requestSender = RequestFactory.CreateRequestSender(
                    logger, 
                    metaCache, 
                    blobCache, 
                    requestSenderSettings);

                var policyCache = CacheFactory.CreateCache(
                    crawlerSettings.ServiceName,
                    logger,
                    policyCacheSettings);

                var sitePolicyResolver = new SitePolicyResolver(
                    logger, 
                    policyCache, 
                    requestSender, 
                    crawlerSettings);

                var crawler = CrawlerFactory.CreateCrawler(
                    logger, 
                    eventBus, 
                    requestSender, 
                    sitePolicyResolver, 
                    crawlerSettings);

                return crawler;
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
                Log.Fatal(ex, $"{crawlerSettings.ServiceName} terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
        }
    }
}
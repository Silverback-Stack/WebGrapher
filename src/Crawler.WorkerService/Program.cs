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


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configCrawler, crawlerSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });


            // Register Crawler Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageCrawler>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    crawlerSettings.ServiceName,
                    logger,
                    metaCacheSettings);

                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    crawlerSettings.ServiceName,
                    logger,
                    blobCacheSettings);

                // Create Request Sender
                var requestSender = RequestFactory.Create(
                    logger, 
                    metaCache, 
                    blobCache, 
                    requestSenderSettings);

                // Create Policy Cache for Site Policy Resolver
                var policyCache = CacheFactory.Create(
                    crawlerSettings.ServiceName,
                    logger,
                    policyCacheSettings);

                // Create Site Policy Resolver
                var sitePolicyResolver = new SitePolicyResolver(
                    logger, 
                    policyCache, 
                    requestSender, 
                    crawlerSettings);

                // Create Crawler Service
                var crawlerService = CrawlerFactory.Create(
                    logger, 
                    eventBus, 
                    requestSender, 
                    sitePolicyResolver, 
                    crawlerSettings);

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
                Log.Fatal(ex, $"{crawlerSettings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
        }
    }
}
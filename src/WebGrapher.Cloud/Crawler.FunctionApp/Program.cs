using System.Configuration;
using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Settings.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Load configuration (shared config)
        var config = ConfigurationLoader.LoadConfiguration("Service.Events");
        var eventBusSettings = config.BindSection<EventBusSettings>("EventBus");


        // Setup Application Insights Logging
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING",
            config["Logging:ApplicationInsights:ConnectionString"]);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddApplicationInsights();
        });

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();


        // Setup Event Bus DI
        services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IEventBus>>();
            return EventBusFactory.CreateEventBus(logger, eventBusSettings);
        });


        // Setup Crawler DI
        services.AddSingleton<IPageCrawler>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IPageCrawler>>();
            var eventBus = sp.GetRequiredService<IEventBus>();

            var config = ConfigurationLoader.LoadConfiguration("Service.Crawler");
            var crawlerSettings = config.BindSection<CrawlerSettings>("Crawler");
            var metaCacheSettings = config.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = config.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = config.BindSection<RequestSenderSettings>("RequestSender");
            var policyCacheSettings = config.BindSection<CacheSettings>("PolicyCache");

            // Create service dependencies
            var metaCache = CacheFactory.CreateCache(crawlerSettings.ServiceName, logger, metaCacheSettings);
            var blobCache = CacheFactory.CreateCache(crawlerSettings.ServiceName, logger, blobCacheSettings);
            var requestSender = RequestFactory.CreateRequestSender(logger, metaCache, blobCache, requestSenderSettings);
            var policyCache = CacheFactory.CreateCache(crawlerSettings.ServiceName, logger, policyCacheSettings);
            var sitePolicyResolver = new SitePolicyResolver(logger, policyCache, requestSender, crawlerSettings);

            var crawler = CrawlerFactory.CreateCrawler(logger, eventBus, requestSender, sitePolicyResolver, crawlerSettings);

            logger.LogInformation("{ServiceName} service started.", crawlerSettings.ServiceName);

            return crawler;
        });

    })
    .Build();

host.Run();

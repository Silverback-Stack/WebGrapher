using Crawler.Core;
using Events.Core.Bus;
using Settings.Core;
using System;

namespace Crawler.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageCrawler _pageCrawler;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageCrawler pageCrawler,
            IConfiguration configuration)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageCrawler = pageCrawler;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.", 
                _configuration.GetEnvironmentName());

            await _eventBus.StartAsync();

            _logger.LogInformation("Crawler service is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _pageCrawler.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

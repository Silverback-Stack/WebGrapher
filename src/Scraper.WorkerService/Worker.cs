using Events.Core.Bus;
using Scraper.Core;
using Settings.Core;

namespace Scraper.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageScraper _pageScraper;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger,
            IEventBus eventBus,
            IPageScraper pageScraper,
            IConfiguration configuration)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageScraper = pageScraper;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _eventBus.StartAsync();

            _logger.LogInformation("Scraper service is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _pageScraper.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

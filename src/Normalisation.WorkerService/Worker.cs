using Events.Core.Bus;
using Normalisation.Core;
using Settings.Core;

namespace Normalisation.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageNormaliser _pageNormaliser;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageNormaliser pageNormaliser,
            IConfiguration configuration)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageNormaliser = pageNormaliser;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _eventBus.StartAsync();

            _logger.LogInformation("Normalisation service is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _pageNormaliser.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

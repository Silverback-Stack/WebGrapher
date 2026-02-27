using Events.Core.Bus;
using Normalisation.Core;

namespace Normalisation.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageNormaliser _pageNormaliser;
        private readonly IHostEnvironment _hostEnvironment;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageNormaliser pageNormaliser,
            IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageNormaliser = pageNormaliser;
            _hostEnvironment = hostEnvironment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _hostEnvironment.EnvironmentName);

            await _eventBus.StartAsync();

            _logger.LogInformation("Normalisation service is starting using {EnvironmentName} configuration.",
                _hostEnvironment.EnvironmentName);

            await _pageNormaliser.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

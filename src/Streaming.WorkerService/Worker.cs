using Events.Core.Bus;
using Streaming.Core;
using Settings.Core;

namespace Streaming.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IGraphStreamer _graphStreamer;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IGraphStreamer graphStreamer,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _eventBus = eventBus;
            _graphStreamer = graphStreamer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            // Start the Event Bus
            await _eventBus.StartAsync();

            _logger.LogInformation("Streaming service is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            // Start the streaming service
            await _graphStreamer.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

using Events.Core.Bus;
using Streaming.Core;
using Settings.Core;
using Streaming.WebApi;

namespace Streaming.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IGraphStreamer _graphStreamer;
        private readonly IConfiguration _configuration;
        private readonly StreamingWebApiSettings _streamingWebApiSettings;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IGraphStreamer graphStreamer,
            IConfiguration configuration,
            StreamingWebApiSettings streamingWebApiSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _eventBus = eventBus;
            _graphStreamer = graphStreamer;
            _streamingWebApiSettings = streamingWebApiSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            // Start the Event Bus
            await _eventBus.StartAsync();

            _logger.LogInformation("Streaming service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                _configuration.GetEnvironmentName(),
                _streamingWebApiSettings.Host,
                _streamingWebApiSettings.Swagger.RoutePrefix);

            // Start the streaming service
            await _graphStreamer.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

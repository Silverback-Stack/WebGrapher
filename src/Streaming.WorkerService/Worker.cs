using Events.Core.Bus;
using Streaming.Core;
using Streaming.WebApi;

namespace Streaming.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IGraphStreamer _graphStreamer;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly StreamingWebApiConfig _streamingWebApiConfig;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IGraphStreamer graphStreamer,
            IHostEnvironment hostEnvironment,
            StreamingWebApiConfig streamingWebApiConfig)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _eventBus = eventBus;
            _graphStreamer = graphStreamer;
            _streamingWebApiConfig = streamingWebApiConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _hostEnvironment.EnvironmentName);

            // Start the Event Bus
            await _eventBus.StartAsync();

            _logger.LogInformation("Streaming service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                _hostEnvironment.EnvironmentName,
                _streamingWebApiConfig.Host,
                _streamingWebApiConfig.Swagger.RoutePrefix);

            // Start the streaming service
            await _graphStreamer.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

using Events.Core.Bus;
using Graphing.Core;
using Settings.Core;
using Graphing.WebApi;

namespace Graphing.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageGrapher _pageGrapher;
        private readonly IConfiguration _configuration;
        private readonly GraphingWebApiSettings _graphingWebApiSettings;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageGrapher pageGrapher,
            IConfiguration configuration,
            GraphingWebApiSettings graphingWebApiSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageGrapher = pageGrapher;
            _configuration = configuration;
            _graphingWebApiSettings = graphingWebApiSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _eventBus.StartAsync();

            _logger.LogInformation("Graphing service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                _configuration.GetEnvironmentName(),
                _graphingWebApiSettings.Host,
                _graphingWebApiSettings.Swagger.RoutePrefix);

            await _pageGrapher.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

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
        private readonly WebApiSettings _webApiSettings;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageGrapher pageGrapher,
            IConfiguration configuration,
            WebApiSettings webApiSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageGrapher = pageGrapher;
            _configuration = configuration;
            _webApiSettings = webApiSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _configuration.GetEnvironmentName());

            await _eventBus.StartAsync();

            _logger.LogInformation("Graphing service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                _configuration.GetEnvironmentName(),
                _webApiSettings.Host,
                _webApiSettings.SwaggerRoutePrefix);

            await _pageGrapher.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

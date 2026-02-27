using Events.Core.Bus;
using Graphing.Core;
using Graphing.WebApi;

namespace Graphing.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus;
        private readonly IPageGrapher _pageGrapher;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly GraphingWebApiConfig _graphingWebApiConfig;

        public Worker(
            ILogger<Worker> logger,
            IEventBus eventBus,
            IPageGrapher pageGrapher,
            IHostEnvironment hostEnvironment,
            GraphingWebApiConfig graphingWebApiConfig)
        {
            _logger = logger;
            _eventBus = eventBus;
            _pageGrapher = pageGrapher;
            _hostEnvironment = hostEnvironment;
            _graphingWebApiConfig = graphingWebApiConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event bus is starting using {EnvironmentName} configuration.",
                _hostEnvironment.EnvironmentName);

            await _eventBus.StartAsync();

            _logger.LogInformation("Graphing service is starting using {EnvironmentName} configuration on {Host}/{Swagger}",
                _hostEnvironment.EnvironmentName,
                _graphingWebApiConfig.Host,
                _graphingWebApiConfig.Swagger.RoutePrefix);

            await _pageGrapher.StartAsync();

            // Keep running until cancellation requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

using System;
using Events.Core.Bus;
using Graphing.Core;
using Graphing.Core.WebGraph.Adapters.InMemory;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Graphing
{
    internal class GraphingService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //SETUP LOGGING:
            var serviceName = typeof(GraphingService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageGrapher>();

            //SETUP WEBGRAPH:
            var webGraph = new InMemoryWebGraphAdapter(logger);

            //CREATE SERVICE:
            GraphingFactory.Create(logger, eventBus, webGraph);

            logger.LogInformation("Graphing service started.");
        }
    }
}

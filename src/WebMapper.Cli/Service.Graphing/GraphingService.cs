using System;
using Events.Core.Bus;
using Graphing.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Graphing
{
    internal class GraphingService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(GraphingService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Debug)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IGraph>();

            GraphingFactory.Create(logger, eventBus);

            logger.LogInformation("Graphing service started.");
        }
    }
}

using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Parser.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Parser
{
    internal class ParserService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(ParserService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IPageParser>();

            ParserFactory.CreateParser(logger, eventBus);

            logger.LogInformation("Parser service started.");
        }
    }
}

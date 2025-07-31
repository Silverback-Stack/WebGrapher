using System;
using Events.Core.Bus;
using Normalisation.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Normalisation
{
    internal class NormalisationService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(NormalisationService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IHtmlNormalisation>();

            NormalisationFactory.CreateNormaliser(logger, eventBus);
        }
    }
}

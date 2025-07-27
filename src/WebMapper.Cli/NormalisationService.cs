using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using Normalisation.Core;
using Serilog;

namespace WebMapper.Cli
{
    internal class NormalisationService
    {
        public static async Task StartAsync(IEventBus eventBus)
        {
            var normalisationLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/normalisation.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var normalisationLogger = LoggerFactory.CreateLogger(
                "NormalisationService",
                LoggerOptions.Serilog,
                normalisationLoggerConfig
            );

            NormalisationFactory.CreateNormaliser(normalisationLogger, eventBus);
        }
    }
}

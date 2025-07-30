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
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(NormalisationService).Name;

            var normalisationLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/normalisation.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var normalisationLogger = Logging.Core.LoggerFactory.CreateLogger(
                serviceName,
                LoggerOptions.Serilog,
                normalisationLoggerConfig
            );

            NormalisationFactory.CreateNormaliser(normalisationLogger, eventBus);
        }
    }
}

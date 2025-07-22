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
        public static async Task ConfigureAsync(IEventBus eventBus)
        {
            var normalisationLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/normalisation.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

            var normalisationLogger = LoggerFactory.CreateLogger(
                "NormalisationService",
                LoggerOptions.Serilog,
                normalisationLoggerConfig
            );

            var service = NormalisationFactory.CreateNormaliser(normalisationLogger, eventBus);
            service.Start();
        }
    }
}

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
            using var normalisationLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/normalisation.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            var normalisationLogger = AppLoggerFactory.CreateLogger(
                "NormalisationService",
                AppLoggerOptions.Serilog,
                normalisationLoggerConfig
            );

            var service = NormalisationFactory.CreateNormaliser(normalisationLogger, eventBus);
            await service.StartAsync();
        }
    }
}

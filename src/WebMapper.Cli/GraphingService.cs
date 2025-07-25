using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Graphing.Core;
using Logging.Core;
using Serilog;

namespace WebMapper.Cli
{
    internal class GraphingService
    {
        public static async Task StartAsync(IEventBus eventBus)
        {
            using var graphingLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/parser.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            using var graphingLogger = LoggerFactory.CreateLogger(
                "ParserService",
                LoggerOptions.Serilog,
                graphingLoggerConfig
            );

            GraphingFactory.Create(graphingLogger, eventBus);
        }
    }
}

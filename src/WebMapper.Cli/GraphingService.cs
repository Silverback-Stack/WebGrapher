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
        public static async Task ConfigureAsync(IEventBus eventBus)
        {
            var graphingLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/parser.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

            var graphingLogger = LoggerFactory.CreateLogger(
                "ParserService",
                LoggerOptions.Serilog,
                graphingLoggerConfig
            );

            var service = GraphingFactory.Create(graphingLogger, eventBus);
            service.Start();
        }
    }
}

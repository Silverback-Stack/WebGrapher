using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using ParserService;
using Serilog;

namespace WebMapper.Cli
{
    internal class ParserService
    {
        public static async Task StartAsync(IEventBus eventBus) {

            using var parserLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/parser.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            using var parserLogger = LoggerFactory.CreateLogger(
                "ParserService",
            LoggerOptions.Serilog,
            parserLoggerConfig
            );

            ParserFactory.CreateParser(parserLogger, eventBus);
        }
    }
}

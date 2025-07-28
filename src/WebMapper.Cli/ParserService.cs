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
        public static async Task InitializeAsync(IEventBus eventBus) 
        {
            var serviceName = typeof(ParserService).Name;

            var parserLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/parser.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var parserLogger = LoggerFactory.CreateLogger(
                serviceName,
                LoggerOptions.Serilog,
                parserLoggerConfig
            );

            ParserFactory.CreateParser(parserLogger, eventBus);
        }
    }
}

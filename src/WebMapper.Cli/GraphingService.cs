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
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(GraphingService).Name;

            var graphingLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/grapher.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var graphingLogger = LoggerFactory.CreateLogger(
                serviceName,
                LoggerOptions.Serilog,
                graphingLoggerConfig
            );

            GraphingFactory.Create(graphingLogger, eventBus);
        }
    }
}

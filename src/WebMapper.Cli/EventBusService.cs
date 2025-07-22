using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using Serilog;

namespace WebMapper.Cli
{
    internal class EventBusService
    {
        public static IEventBus Start()
        {
            using var eventLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/eventbus.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var eventLogger = AppLoggerFactory.CreateLogger(
                "EventsService",
                AppLoggerOptions.Serilog,
                eventLoggerConfig);
            return EventBusFactory.CreateEventBus(eventLogger);
        }
    }
}

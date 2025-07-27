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
            var eventLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/eventbus.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            var eventLogger = LoggerFactory.CreateLogger(
                "EventsService",
                LoggerOptions.Serilog,
                eventLoggerConfig);

            var eventBus = EventBusFactory.CreateEventBus(eventLogger);
            eventBus.StartAsync();
            return eventBus;
        }
    }
}

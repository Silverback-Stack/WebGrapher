using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using Microsoft.AspNetCore.SignalR;
using Streaming.Core.Adapters.SignalR;

namespace Streaming.Core
{
    public class StreamerFactory
    {
        public static IGraphStreamer Create
            (ILogger logger, 
            IEventBus eventBus,
            IHubContext<GraphHub> hubContext)
        {
            var service = new SignalRGraphAdapter(logger, eventBus, hubContext);
            service.SubscribeAll();
            return service;
        }
    }
}

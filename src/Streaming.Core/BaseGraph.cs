using System;
using Events.Core.Bus;
using Logging.Core;
using Streaming.Core.Models;

namespace Streaming.Core
{
    public abstract class BaseGraph : IGraphStreamer, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        public BaseGraph(
            ILogger logger, 
            IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            //throw new NotImplementedException();
        }

        public void UnsubscribeAll()
        {
            //throw new NotImplementedException();
        }

        public abstract Task StreamNodeAsync(PageNode node, Guid? graphId = null);
        public abstract Task StreamEdgeAsync(PageEdge edge, Guid? graphId = null);

        public abstract Task StreamGraphAsync(Guid? graphId = null);

        public abstract Task BroadcastMessageAsync(string message, Guid? graphId = null);

        public abstract Task BroadcastMetricsAsync(Guid? graphId = null);
    }
}

using System;
using Events.Core.Bus;
using Graphing.Core.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.Adapters.AzureCosmoGraph
{
    public class AzureCosmoGraphAdapter : BaseGraph
    {
        private readonly IGraphAnalyser _graphAnalyser;

        public AzureCosmoGraphAdapter(ILogger logger, IEventBus eventBus) : base(logger, eventBus)
        {
            _graphAnalyser = new AzureCosmoGraphAnalyserAdapter();
        }

        public override IGraphAnalyser GraphAnalyser => throw new NotImplementedException();

        public override Node AddNode(string id, string title, string keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool IsNodePopulated(string id)
        {
            throw new NotImplementedException();
        }

        public override void RemoveNode(string id)
        {
            throw new NotImplementedException();
        }
    }
}

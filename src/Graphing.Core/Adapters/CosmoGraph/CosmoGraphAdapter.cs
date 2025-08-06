//using System;
//using Events.Core.Bus;
//using Graphing.Core.Models;
//using Microsoft.Extensions.Logging;

//namespace Graphing.Core.Adapters.CosmoGraph
//{
//    public class CosmoGraphAdapter : BaseGraph
//    {
//        private readonly IGraphAnalyser _graphAnalyser;

//        public CosmoGraphAdapter(ILogger logger, IEventBus eventBus) : base(logger, eventBus)
//        {
//            _graphAnalyser = new CosmoGraphAnalyserAdapter();
//        }

//        public override IGraphAnalyser GraphAnalyser => throw new NotImplementedException();

//        public override Task DeleteNodeAsync(string id)
//        {
//            throw new NotImplementedException();
//        }

//        public override void Dispose()
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<Node?> GetNodeAsync(string id)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<Node?> SetNodeAsync(Node node)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

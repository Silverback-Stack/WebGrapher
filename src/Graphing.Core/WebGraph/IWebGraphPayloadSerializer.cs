using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;
using System;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraphPayloadSerializer
    {
        SigmaGraphPayloadDto Serialize(IEnumerable<Node> nodes, Guid graphId);
        SigmaGraphPayloadDto Serialize(Node node);
        SigmaGraphPayloadDto Empty(Guid graphId);
    }
}

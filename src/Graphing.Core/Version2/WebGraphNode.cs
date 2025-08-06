using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.Version2
{
    public class WebGraphNode
    {
        public int GraphId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public NodeState State { get; set; }
        public HashSet<WebGraphNode> OutgoingLinks { get; set; } = new();
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastScheduledAt { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }

        public WebGraphNode() { }

        public WebGraphNode(int graphId, string url, NodeState state = NodeState.Dummy) {
            GraphId = graphId;
            Url = url;
            State = state;
        }
    }
}

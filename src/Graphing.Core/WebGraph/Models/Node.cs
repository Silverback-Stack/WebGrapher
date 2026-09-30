namespace Graphing.Core.WebGraph.Models
{
    public class Node
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GraphId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool ImageCors { get; set; } = true;
        public string Keywords { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
        public NodeState State { get; set; }
        public string RedirectedToUrl { get; set; }
        public HashSet<Node> OutgoingNodes { get; set; } = new();
        public HashSet<Node> IncomingNodes { get; set; } = new();
        public int PopularityScore { get; set; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? SourceLastModified { get; set; }

        /// <summary>
        /// Earliest time another refresh is permitted.
        /// </summary>
        public DateTimeOffset? AllowRefreshAfter { get; set; } 

        public string ContentFingerprint { get; set; } = string.Empty;

        public Node() { }

        public Node(Guid graphId, string url, NodeState state = NodeState.Dummy)
        {
            GraphId = graphId;
            Url = url;
            State = state;
        }

        public int OutgoingNodeCount => OutgoingNodes.Count();
        public int IncomingNodeCount => IncomingNodes.Count();
    }
}

namespace Graphing.Core.WebGraph.Models
{
    public class Node
    {
        public int GraphId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summery { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
        public NodeState State { get; set; }
        public string RedirectedToUrl { get; set; }
        public HashSet<Node> OutgoingLinks { get; set; } = new();
        public HashSet<Node> IncomingLinks { get; set; } = new();
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastScheduledAt { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }

        public Node() { }

        public Node(int graphId, string url, NodeState state = NodeState.Dummy)
        {
            GraphId = graphId;
            Url = url;
            State = state;
        }

        public int OutgoingLinkCount => OutgoingLinks.Count();
        public int IncomingLinkCount => IncomingLinks.Count();
    }
}

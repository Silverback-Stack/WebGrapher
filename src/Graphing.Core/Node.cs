using System.ComponentModel;
using System.Data;

namespace Graphing.Core
{
    public class Node
    {
        public string Id { get; }
        public string Title { get; set; }
        public string Keywords { get; set; }
        public HashSet<string> Edges { get; set; } = new();
        public DateTimeOffset? SourceLastModifed { get; set; }
        public DateTimeOffset CreatedAt { get; }

        public Node(string id)
        {
            Id = id;
            Title = string.Empty;
            Keywords = string.Empty;
            SourceLastModifed = null;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public Node(string id, string title, string keywords, DateTimeOffset sourceLastModified, IEnumerable<string> edges)
        {
            Id = id;
            Title = title;
            Keywords = keywords;
            SourceLastModifed = sourceLastModified;
            CreatedAt = DateTimeOffset.UtcNow;
            SetEdges(edges);
        }

        public bool HasData => SourceLastModifed != null;

        public void SetEdges(IEnumerable<string> edges)
        {
            Edges.Clear();
            foreach (var edge in edges)
            {
                Edges.Add(edge);
            }
        }

    }
}

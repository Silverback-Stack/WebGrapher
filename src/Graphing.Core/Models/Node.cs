
using System.Resources;
using System.Xml.Linq;

namespace Graphing.Core.Models
{
    public class Node
    {
        public string Id { get; private init; }
        public string? Title { get; set; }
        public string? Keywords { get; set; }
        public HashSet<Edge> Edges { get; set; } = new();
        public DateTimeOffset? SourceLastModifed { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public DateTimeOffset CreatedAt { get; init; }
        public NodeState State { get; private set; }
        public string? RedirectsTo { get; private set; }

        public Node(string id) 
        { 
            Id = id;
            ModifiedAt = DateTimeOffset.UtcNow;
            CreatedAt = DateTimeOffset.UtcNow;
            State = NodeState.Dummy;
        }

        public Node(string id, string? title, string? keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges)
        { 
            Id = id;
            Title = title;
            Keywords = keywords;
            SourceLastModifed = sourceLastModified;
            ModifiedAt = DateTimeOffset.UtcNow;
            CreatedAt = DateTimeOffset.UtcNow;
            State = NodeState.Populated;

            SetEdges(edges);
        }

        public void SetRedirected(string redirectedTo)
        {
            State = NodeState.Redirected;
            RedirectsTo = redirectedTo;
            ModifiedAt = DateTimeOffset.UtcNow;

            Edges.RemoveWhere(e => e.State == EdgeState.FromSource);
        }

        public void SetPopulated(string? title, string? keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges)
        {
            Title = title;
            Keywords = keywords;
            SourceLastModifed = sourceLastModified;
            ModifiedAt = DateTimeOffset.UtcNow;
            State = NodeState.Populated;
            SetEdges(edges);
        }

        public bool HasData => State == NodeState.Populated;

        public void SetEdges(IEnumerable<string> edges)
        {
            var originalEdgesIds = Edges.Where(e => e.State == EdgeState.FromSource).Select(e => e.Id).ToHashSet();

            Edges.RemoveWhere(e => e.State == EdgeState.FromSource);

            //remove orphaned redirect edges
            Edges.RemoveWhere(e => 
                e.State == EdgeState.FromRedirect 
                && e.RedirectedFrom != null 
                && originalEdgesIds.Contains(e.RedirectedFrom));

            foreach (var edge in edges)
            {
                Edges.Add(new Edge { Id = edge, State = EdgeState.FromSource });
            }
        }
        public void AddRedirectEdge(string target, string redirectedFrom)
        {
            Edges.Add(new Edge { Id = target, State = EdgeState.FromRedirect, RedirectedFrom = redirectedFrom });
        }

        public bool IsStale(int maxDays)
        {
            return ModifiedAt < DateTimeOffset.UtcNow.AddDays(-maxDays);
        }

    }
}

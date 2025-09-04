using System;
using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaJsGraphSerializer
    {

        public static (IReadOnlyList<SigmaGraphNodeDto> Nodes, IReadOnlyList<SigmaGraphEdgeDto> Edges)
            GetPopulationDelta(Node node, GraphingSettings graphingSettings)
        {
            if (node.State != NodeState.Populated)
                return (Array.Empty<SigmaGraphNodeDto>(), Array.Empty<SigmaGraphEdgeDto>());

            var nodes = new List<SigmaGraphNodeDto> { CreateNodeDto(node, graphingSettings) };
            var edges = new List<SigmaGraphEdgeDto>();
            var seenEdges = new HashSet<string>();

            void AddEdge(string source, string target)
            {
                if (source == target) return; // skip self-loop edges

                var id = $"{source}->{target}";
                if (seenEdges.Add(id))
                {
                    edges.Add(CreateEdgeDto(source, target));
                }
            }

            // 1. Outgoing edges to populated nodes
            foreach (var target in node.OutgoingLinks.Where(n => n.State == NodeState.Populated))
                AddEdge(node.Url, target.Url);

            // 2. Incoming edges from populated nodes
            foreach (var source in node.IncomingLinks.Where(n => n.State == NodeState.Populated))
                AddEdge(source.Url, node.Url);

            // 3. Incoming edges from Redirect nodes
            foreach (var redirect in node.IncomingLinks.Where(n => n.State == NodeState.Redirected))
                foreach (var fromNode in redirect.IncomingLinks.Where(n => n.State == NodeState.Populated))
                    AddEdge(fromNode.Url, node.Url);

            // 4. Outgoing edges to Redirect nodes
            foreach (var redirect in node.OutgoingLinks.Where(n => n.State == NodeState.Redirected &&
                                                                   !string.IsNullOrWhiteSpace(n.RedirectedToUrl)))
                AddEdge(node.Url, redirect.RedirectedToUrl);

            return (nodes, edges);
        }


        private static SigmaGraphNodeDto CreateNodeDto(Node node, GraphingSettings graphingSettings) => 
            new SigmaGraphNodeDto
            {
                Id = node.Url,
                Label = node.Title,
                PopularityScore = node.PopularityScore,
                Size = CalculateNodeSize(node.PopularityScore, graphingSettings),
                State = node.State.ToString(), 
                Summary = node.Summary,
                Image = node.ImageUrl,
                Tags = node.Tags,
                CreatedAt = node.CreatedAt,
                SourceLastModified = node.SourceLastModified
            };

        private static SigmaGraphEdgeDto CreateEdgeDto(string sourceUrl, string targetUrl)
        {
            return new SigmaGraphEdgeDto
            {
                Id = $"{sourceUrl}->{targetUrl}",
                Source = sourceUrl,
                Target = targetUrl
            };
        }

        private static double CalculateNodeSize(int popularityScore, GraphingSettings graphingSettings)
        {
            var minSize = graphingSettings.WebGraph.SigmaJsGraph.MinNodeSize;
            var maxSize = graphingSettings.WebGraph.SigmaJsGraph.MaxNodeSize;

            // Logarithmic scaling: keeps huge counts from exploding
            return minSize +
                (Math.Log10(popularityScore + 1) * (maxSize - minSize) / Math.Log10(1000));
        }
    }
}

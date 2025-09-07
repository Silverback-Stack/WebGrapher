using System;
using Graphing.Core;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Adapters.InMemory;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Service.Graphing.Tests
{
    [TestFixture]
    public class InMemoryWebGraphAdapterTests
    {
        private InMemoryWebGraphAdapter _adapter;
        private Mock<ILogger> _logger;
        private bool _includeImmediateNeighborhood;
        private GraphingSettings _graphingSettings;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _graphingSettings = new GraphingSettings();
            _adapter = new InMemoryWebGraphAdapter(_logger.Object, _graphingSettings);
            _includeImmediateNeighborhood = true; //true when current link depth < 2
        }

        [Test]
        public async Task GetMostPopularNodesAsync_ReturnsTopNodesOrderedByPopularityScoreAndCreatedAt()
        {
            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            // Helper to create a node with N incoming and optional outgoing links
            Node CreateNode(string url, int incomingLinks, int outgoingLinks, DateTimeOffset createdAt)
            {
                var node = new Node(graphId, url)
                {
                    State = NodeState.Populated,
                    CreatedAt = createdAt,
                    ModifiedAt = createdAt
                };

                // Populate incoming links
                for (int i = 0; i < incomingLinks; i++)
                    node.IncomingLinks.Add(new Node(graphId, $"{url}_incoming_{i}"));

                // Populate outgoing links
                for (int i = 0; i < outgoingLinks; i++)
                    node.OutgoingLinks.Add(new Node(graphId, $"{url}_outgoing_{i}"));

                // Update popularity score explicitly
                node.PopularityScore = node.IncomingLinks.Count + node.OutgoingLinks.Count;

                return node;
            }

            var node1 = CreateNode("url1", 5, 0, DateTimeOffset.UtcNow.AddHours(-2));  // score = 5
            var node2 = CreateNode("url2", 10, 0, DateTimeOffset.UtcNow.AddHours(-3)); // score = 10
            var node3 = CreateNode("url3", 10, 0, DateTimeOffset.UtcNow.AddHours(-1)); // score = 10
            var node4 = CreateNode("url4", 1, 0, DateTimeOffset.UtcNow);  // score = 1

            await _adapter.SetNodeAsync(node1);
            await _adapter.SetNodeAsync(node2);
            await _adapter.SetNodeAsync(node3);
            await _adapter.SetNodeAsync(node4);

            var result = await _adapter.GetMostPopularNodes(graphId, 2);

            Assert.That(result.Count(), Is.EqualTo(2));

            // Tie on popularity score, newer ModifiedAt wins
            Assert.That(result.ElementAt(0).Url, Is.EqualTo("url3"));
            Assert.That(result.ElementAt(1).Url, Is.EqualTo("url2"));
        }



        [Test]
        public async Task TraverseGraphAsync_TraversesGraphUpToMaxDepth()
        {
            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");

            nodeA.OutgoingLinks.Add(nodeB);
            nodeB.OutgoingLinks.Add(nodeC);
            nodeC.OutgoingLinks.Add(nodeD);

            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);

            // Depth 0 = only A
            var resultDepth0 = await _adapter.TraverseGraphAsync(graphId, "A", maxDepth: 0);
            Assert.That(resultDepth0.Select(n => n.Url), Is.EquivalentTo(new[] { "A" }));

            // Depth 1 = A, B
            var resultDepth1 = await _adapter.TraverseGraphAsync(graphId, "A", maxDepth: 1);
            Assert.That(resultDepth1.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B" }));

            // Depth 2 = A, B, C
            var resultDepth2 = await _adapter.TraverseGraphAsync(graphId, "A", maxDepth: 2);
            Assert.That(resultDepth2.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B", "C" }));

            // Depth 3 = A, B, C, D
            var resultDepth3 = await _adapter.TraverseGraphAsync(graphId, "A", maxDepth: 3);
            Assert.That(resultDepth3.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
        }

        [Test]
        public async Task TraverseGraphAsync_StopsAtMaxNodesLimit()
        {
            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            //create a bunch of nodes:
            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");
            var nodeE = new Node(graphId, "E");
            var nodeF = new Node(graphId, "F");

            // Create a star burst: A -> B,C,D,E,F
            nodeA.OutgoingLinks.Add(nodeB);
            nodeA.OutgoingLinks.Add(nodeC);
            nodeA.OutgoingLinks.Add(nodeD);
            nodeA.OutgoingLinks.Add(nodeE);
            nodeA.OutgoingLinks.Add(nodeF);

            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);
            await _adapter.SetNodeAsync(nodeE);
            await _adapter.SetNodeAsync(nodeF);

            // maxNodes = 3 should only return A plus two others
            var result = await _adapter.TraverseGraphAsync(graphId, "A", maxDepth: 1, maxNodes: 3);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result, Has.Some.Matches<Node>(n => n.Url == "A"));
        }


        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_AppendMode_DoesNotRemoveExistingIncomingLinks()
        {
            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");
            var linkUpdateMode = NodeEdgesUpdateMode.Append;

            var webPageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };
            var webPageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            await _adapter.AddWebPageAsync(webPageA, _includeImmediateNeighborhood, null, null, linkUpdateMode);
            await _adapter.AddWebPageAsync(webPageB, _includeImmediateNeighborhood, null, null, linkUpdateMode);

            var nodeB = await _adapter.GetNodeAsync(graphId, "B");
            Assert.That(nodeB.IncomingLinkCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingLinks.Any(n => n.Url == "A"), Is.True);

            // revisit A with no links in Append mode
            var webPageARevisited = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string>(),
                SourceLastModified = DateTimeOffset.UtcNow,
                ContentFingerprint = "HASH-A-REVISITED"
            };

            await _adapter.AddWebPageAsync(webPageARevisited, _includeImmediateNeighborhood,  null, null, linkUpdateMode);

            nodeB = await _adapter.GetNodeAsync(graphId, "B");
            // Append mode: old links are kept
            Assert.That(nodeB.IncomingLinkCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingLinks.Any(n => n.Url == "A"), Is.True);
        }

        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_ReplaceMode_RemovesOldIncomingLinks()
        {
            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");
            var linkUpdateMode = NodeEdgesUpdateMode.Replace;

            var webPageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };
            var webPageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            await _adapter.AddWebPageAsync(webPageA, _includeImmediateNeighborhood, null, null, linkUpdateMode);
            await _adapter.AddWebPageAsync(webPageB, _includeImmediateNeighborhood, null, null, linkUpdateMode);

            var nodeB = await _adapter.GetNodeAsync(graphId, "B");
            Assert.That(nodeB.IncomingLinkCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingLinks.Any(n => n.Url == "A"), Is.True);

            // revisit A with no links in Replace mode
            var webPageARevisited = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string>(),
                SourceLastModified = DateTimeOffset.UtcNow,
                ContentFingerprint = "HASH-A-REVISITED"
            };

            await _adapter.AddWebPageAsync(webPageARevisited, _includeImmediateNeighborhood, null, null, linkUpdateMode);

            nodeB = await _adapter.GetNodeAsync(graphId, "B");
            // Replace mode: old links removed
            Assert.That(nodeB.IncomingLinkCount, Is.EqualTo(0));
            Assert.That(nodeB.IncomingLinks.Any(n => n.Url == "A"), Is.False);
        }

    }
}

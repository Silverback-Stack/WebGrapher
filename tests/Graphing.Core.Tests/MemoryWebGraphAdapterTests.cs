using System;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Graphing.Infrastructure.WebGraph.Adapters.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Graphing.Core.Tests
{
    [TestFixture]
    public class MemoryWebGraphAdapterTests
    {
        private MemoryWebGraphAdapter _adapter;
        private Mock<ILogger> _logger;
        private GraphingSettings _graphingSettings;

        // Depth 0 simulates an initial crawl, bypassing refresh throttling
        // and ensuring existing relationships are processed.
        private const int CrawlDepth = 0;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _graphingSettings = new GraphingSettings();
        }

        /// <summary>
        /// Returns the first populated Nodes for the initial graph payload.
        /// </summary>
        [Test]
        public async Task GetInitialGraphNodes_ReturnsFirstPopulatedNodesUpToTopN()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = OutgoingNodesUpdateMode.Append;
            _adapter = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);

            var graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            var node1 = new Node(graphId, "url1")
            {
                State = NodeState.Populated,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            };

            var node2 = new Node(graphId, "url2")
            {
                State = NodeState.Populated,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
            };

            var node3 = new Node(graphId, "url3")
            {
                State = NodeState.Populated,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            };

            var node4 = new Node(graphId, "url4")
            {
                State = NodeState.Populated,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _adapter.SetNodeAsync(node1);
            await _adapter.SetNodeAsync(node2);
            await _adapter.SetNodeAsync(node3);
            await _adapter.SetNodeAsync(node4);

            var result = (await _adapter.GetInitialGraphNodes(graphId, 2)).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Url, Is.EqualTo("url2"));
            Assert.That(result[1].Url, Is.EqualTo("url1"));
        }



        [Test]
        public async Task TraverseGraphAsync_TraversesGraphUpToMaxDepth()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = OutgoingNodesUpdateMode.Append;
            _adapter = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);

            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");

            nodeA.OutgoingNodes.Add(nodeB);
            nodeB.OutgoingNodes.Add(nodeC);
            nodeC.OutgoingNodes.Add(nodeD);

            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);

            // Depth 0 = only A
            var resultDepth0 = await _adapter.GetNodeNeighborhoodAsync(graphId, "A", maxDepth: 0);
            Assert.That(resultDepth0.Select(n => n.Url), Is.EquivalentTo(new[] { "A" }));

            // Depth 1 = A, B
            var resultDepth1 = await _adapter.GetNodeNeighborhoodAsync(graphId, "A", maxDepth: 1);
            Assert.That(resultDepth1.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B" }));

            // Depth 2 = A, B, C
            var resultDepth2 = await _adapter.GetNodeNeighborhoodAsync(graphId, "A", maxDepth: 2);
            Assert.That(resultDepth2.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B", "C" }));

            // Depth 3 = A, B, C, D
            var resultDepth3 = await _adapter.GetNodeNeighborhoodAsync(graphId, "A", maxDepth: 3);
            Assert.That(resultDepth3.Select(n => n.Url), Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
        }

        [Test]
        public async Task TraverseGraphAsync_StopsAtMaxNodesLimit()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = OutgoingNodesUpdateMode.Append;
            _adapter = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);

            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            //create a bunch of nodes:
            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");
            var nodeE = new Node(graphId, "E");
            var nodeF = new Node(graphId, "F");

            // Create a star burst: A -> B,C,D,E,F
            nodeA.OutgoingNodes.Add(nodeB);
            nodeA.OutgoingNodes.Add(nodeC);
            nodeA.OutgoingNodes.Add(nodeD);
            nodeA.OutgoingNodes.Add(nodeE);
            nodeA.OutgoingNodes.Add(nodeF);

            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);
            await _adapter.SetNodeAsync(nodeE);
            await _adapter.SetNodeAsync(nodeF);

            // maxNodes = 3 should only return A plus two others
            var result = await _adapter.GetNodeNeighborhoodAsync(graphId, "A", maxDepth: 1, maxNodes: 3);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result, Has.Some.Matches<Node>(n => n.Url == "A"));
        }


        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_AppendMode_DoesNotRemoveExistingIncomingLinks()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = OutgoingNodesUpdateMode.Append;
            _adapter = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);

            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            var webPageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };
            var webPageB = new PageData
            {
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            // Depth 0 simulates an initial crawl, bypassing refresh throttling and processing existing relationships
            await _adapter.MapPageAsync(graphId, webPageA, CrawlDepth, null, null);
            await _adapter.MapPageAsync(graphId, webPageB, CrawlDepth, null, null);

            var nodeB = await _adapter.GetNodeAsync(graphId, "B");
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingNodes.Any(n => n.Url == "A"), Is.True);

            // Revisit A with no outgoing relationships in Append mode
            var webPageARevisited = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string>(),
                SourceLastModified = DateTimeOffset.UtcNow,
                ContentFingerprint = "HASH-A-REVISITED"
            };

            await _adapter.MapPageAsync(
                graphId, 
                webPageARevisited,
                CrawlDepth,  
                null, null);

            nodeB = await _adapter.GetNodeAsync(graphId, "B");

            // Append mode: existing relationships are retained
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingNodes.Any(n => n.Url == "A"), Is.True);
        }

        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_ReplaceMode_RemovesOldIncomingLinks()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = OutgoingNodesUpdateMode.Replace;
            _adapter = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);

            Guid graphId = Guid.Parse("7d0d7fea-adcc-45d3-aafa-5cbb5ce4bc1f");

            var webPageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };
            var webPageB = new PageData
            {
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            // Depth 0 simulates an initial crawl, bypassing refresh throttling and processing existing relationships
            await _adapter.MapPageAsync(graphId, webPageA, CrawlDepth, null, null);
            await _adapter.MapPageAsync(graphId, webPageB, CrawlDepth, null, null);

            var nodeB = await _adapter.GetNodeAsync(graphId, "B");
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(nodeB.IncomingNodes.Any(n => n.Url == "A"), Is.True);

            // Revisit A with no outgoing relationships in Replace mode
            var webPageARevisited = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string>(),
                SourceLastModified = DateTimeOffset.UtcNow,
                ContentFingerprint = "HASH-A-REVISITED"
            };

            await _adapter.MapPageAsync(graphId, webPageARevisited, CrawlDepth, null, null);

            nodeB = await _adapter.GetNodeAsync(graphId, "B");

            // Replace mode: existing relationships are removed
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(0));
            Assert.That(nodeB.IncomingNodes.Any(n => n.Url == "A"), Is.False);
        }

    }
}

using System;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Graphing.Infrastructure.WebGraph.Adapters.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Graphing.Core.Tests
{
    /// <summary>
    /// Tests the in-memory WebGraph adapter and its graph storage,
    /// traversal, and relationship update behaviour.
    /// </summary>
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

        private async Task<Guid> CreateGraphAsync()
        {
            var graphId = Guid.NewGuid();

            await _adapter.CreateGraphAsync(
                graphId,
                "test-user",
                new GraphOptions
                {
                    Name = "Test Graph"
                });

            return graphId;
        }


        /// <summary>
        /// Preserves existing relationships when outgoing relationships are updated in Append mode.
        /// </summary>
        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_AppendMode_DoesNotRemoveExistingIncomingLinks()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = 
                OutgoingNodesUpdateMode.Append;

            _adapter = new MemoryWebGraphAdapter(
                _logger.Object, 
                _graphingSettings);

            var graphId = await CreateGraphAsync();

            // Create Page A with a hyperlink to Page B.
            var webPageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };

            // Create Page B with no outgoing hyperlinks.
            var webPageB = new PageData
            {
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            // Map both pages so the relationship A -> B is created.
            await _adapter.MapPageAsync(
                graphId, 
                webPageA, 
                CrawlDepth, 
                null, 
                null);

            await _adapter.MapPageAsync(
                graphId, 
                webPageB, 
                CrawlDepth, 
                null, 
                null);

            // Verify Node B has an incoming relationship from Node A.
            var nodeB = await _adapter.GetNodeAsync(graphId, "B");

            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(
                nodeB.IncomingNodes.Any(n => n.Url == "A"), 
                Is.True);

            // Revisit Page A with no outgoing hyperlinks.
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

            // Verify Append mode preserves the existing relationship A -> B.
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(
                nodeB.IncomingNodes.Any(n => n.Url == "A"),
                Is.True);
        }


        /// <summary>
        /// Removes obsolete relationships when outgoing relationships are updated in Replace mode.
        /// </summary>
        [Test]
        public async Task UpdatingNodeOutgoingLinksAsync_ReplaceMode_RemovesOldIncomingLinks()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode = 
                OutgoingNodesUpdateMode.Replace;

            _adapter = new MemoryWebGraphAdapter(
                _logger.Object, 
                _graphingSettings);

            var graphId = await CreateGraphAsync();

            // Create Page A with a hyperlink to Page B.
            var webPageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                Links = new List<string> { "B" },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-A"
            };

            // Create Page B with no outgoing hyperlinks.
            var webPageB = new PageData
            {
                Url = "B",
                OriginalUrl = "B",
                Links = new List<string> { },
                SourceLastModified = DateTimeOffset.UtcNow.AddYears(-1),
                ContentFingerprint = "HASH-B"
            };

            // Map both pages so the relationship A -> B is created.
            await _adapter.MapPageAsync(
                graphId, 
                webPageA, 
                CrawlDepth, 
                null, 
                null);

            await _adapter.MapPageAsync(
                graphId, 
                webPageB, 
                CrawlDepth, 
                null, 
                null);

            // Verify Node B has an incoming relationship from Node A.
            var nodeB = await _adapter.GetNodeAsync(graphId, "B");

            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(1));
            Assert.That(
                nodeB.IncomingNodes.Any(n => n.Url == "A"), 
                Is.True);

            // Revisit Page A with no outgoing hyperlinks
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
                null, 
                null);

            nodeB = await _adapter.GetNodeAsync(graphId, "B");

            // Verify Replace mode removes the obsolete relationship A -> B.
            Assert.That(nodeB.IncomingNodeCount, Is.EqualTo(0));
            Assert.That(
                nodeB.IncomingNodes.Any(n => n.Url == "A"), 
                Is.False);
        }




        // ########################
        // Add below in API Chapter
        // ########################

        /// <summary>
        /// Returns the Nodes that initially populated the Graph, up to the requested limit.
        /// </summary>
        [Test]
        public async Task GetInitialGraphNodes_ReturnsFirstPopulatedNodesUpToTopN()
        {
            // Configure the adapter before creating it.
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode =
                OutgoingNodesUpdateMode.Append;

            _adapter = new MemoryWebGraphAdapter(
                _logger.Object,
                _graphingSettings);

            var graphId = await CreateGraphAsync();


            // Create populated Nodes with different creation times.
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

            // Store the Nodes in the in-memory WebGraph.
            await _adapter.SetNodeAsync(node1);
            await _adapter.SetNodeAsync(node2);
            await _adapter.SetNodeAsync(node3);
            await _adapter.SetNodeAsync(node4);

            // Request the first two Nodes that populated the Graph.
            var result = (await _adapter.GetInitialGraphNodes(graphId, 2)).ToList();

            // Verify the requested limit and that the first populated Nodes are returned.
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Url, Is.EqualTo("url2"));
            Assert.That(result[1].Url, Is.EqualTo("url1"));
        }


        /// <summary>
        /// Traverses outgoing relationships only up to the requested maximum depth.
        /// </summary>
        [Test]
        public async Task TraverseGraphAsync_TraversesGraphUpToMaxDepth()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode =
                OutgoingNodesUpdateMode.Append;

            _adapter = new MemoryWebGraphAdapter(
                _logger.Object,
                _graphingSettings);

            Guid graphId = await CreateGraphAsync();

            // Create a simple chain of Nodes: A -> B -> C -> D.
            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");

            // Add the outgoing relationships that define the traversal path.
            nodeA.OutgoingNodes.Add(nodeB);
            nodeB.OutgoingNodes.Add(nodeC);
            nodeC.OutgoingNodes.Add(nodeD);

            // Store the Nodes in the in-memory WebGraph.
            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);

            // Depth 0 = only A
            var resultDepth0 = await _adapter.GetNodeNeighborhoodAsync(
                graphId, "A", maxDepth: 0);

            Assert.That(
                resultDepth0.Select(n => n.Url),
                Is.EquivalentTo(new[] { "A" }));

            // Depth 1 = A, B
            var resultDepth1 = await _adapter.GetNodeNeighborhoodAsync(
                graphId, "A", maxDepth: 1);

            Assert.That(
                resultDepth1.Select(n => n.Url),
                Is.EquivalentTo(new[] { "A", "B" }));

            // Depth 2 = A, B, C
            var resultDepth2 = await _adapter.GetNodeNeighborhoodAsync(
                graphId, "A", maxDepth: 2);

            Assert.That(
                resultDepth2.Select(n => n.Url),
                Is.EquivalentTo(new[] { "A", "B", "C" }));

            // Depth 3 = A, B, C, D
            var resultDepth3 = await _adapter.GetNodeNeighborhoodAsync(
                graphId, "A", maxDepth: 3);

            Assert.That(
                resultDepth3.Select(n => n.Url),
                Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
        }


        /// <summary>
        /// Stops graph traversal when the requested maximum Node count is reached.
        /// </summary>
        [Test]
        public async Task TraverseGraphAsync_StopsAtMaxNodesLimit()
        {
            _graphingSettings.WebGraph.OutgoingNodesUpdateMode =
                OutgoingNodesUpdateMode.Append;

            _adapter = new MemoryWebGraphAdapter(
                _logger.Object,
                _graphingSettings);

            var graphId = await CreateGraphAsync();

            // Create a group of Nodes for the traversal test.
            var nodeA = new Node(graphId, "A");
            var nodeB = new Node(graphId, "B");
            var nodeC = new Node(graphId, "C");
            var nodeD = new Node(graphId, "D");
            var nodeE = new Node(graphId, "E");
            var nodeF = new Node(graphId, "F");

            // Create a star-shaped graph: A -> B, C, D, E, F.
            nodeA.OutgoingNodes.Add(nodeB);
            nodeA.OutgoingNodes.Add(nodeC);
            nodeA.OutgoingNodes.Add(nodeD);
            nodeA.OutgoingNodes.Add(nodeE);
            nodeA.OutgoingNodes.Add(nodeF);

            // Store the Nodes in the in-memory WebGraph.
            await _adapter.SetNodeAsync(nodeA);
            await _adapter.SetNodeAsync(nodeB);
            await _adapter.SetNodeAsync(nodeC);
            await _adapter.SetNodeAsync(nodeD);
            await _adapter.SetNodeAsync(nodeE);
            await _adapter.SetNodeAsync(nodeF);

            // Limit the traversal to three Nodes.
            var result = await _adapter.GetNodeNeighborhoodAsync(
                graphId,
                "A",
                maxDepth: 1,
                maxNodes: 3);

            // Verify the Node limit is respected and the starting Node is included.
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result, Has.Some.Matches<Node>(n => n.Url == "A"));
        }
    }
}

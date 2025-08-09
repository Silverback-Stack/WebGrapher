using System;
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

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _adapter = new InMemoryWebGraphAdapter(_logger.Object);
        }

        [Test]
        public async Task GetMostPopularNodesAsync_ReturnsTopNodesOrderedByIncomingLinkCountAndCreatedAt()
        {
            int graphId = 1;

            var node1 = new Node(graphId, "url1") { IncomingLinkCount = 5, CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) };
            var node2 = new Node(graphId, "url2") { IncomingLinkCount = 10, CreatedAt = DateTimeOffset.UtcNow.AddHours(-3) };
            var node3 = new Node(graphId, "url3") { IncomingLinkCount = 10, CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
            var node4 = new Node(graphId, "url4") { IncomingLinkCount = 1, CreatedAt = DateTimeOffset.UtcNow };

            await _adapter.SetNodeAsync(node1);
            await _adapter.SetNodeAsync(node2);
            await _adapter.SetNodeAsync(node3);
            await _adapter.SetNodeAsync(node4);

            var result = await _adapter.GetMostPopularNodesAsync(graphId, 2);

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.ElementAt(0).Url, Is.EqualTo("url3")); // same IncomingLinkCount but newer CreatedAt wins
            Assert.That(result.ElementAt(1).Url, Is.EqualTo("url2"));
        }

        [Test]
        public async Task TraverseGraphAsync_TraversesGraphUpToMaxDepth()
        {
            int graphId = 1;

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
            int graphId = 1;

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
    }


}

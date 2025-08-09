using Moq;
using Microsoft.Extensions.Logging;
using Graphing.Core.WebGraph.Models;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Adapters.InMemory;

namespace Service.Graphing.Tests
{
    [TestFixture]
    public class BaseWebGraphTests
    {
        private Mock<ILogger> _logger;
        private IWebGraph _webGraph;
        private static readonly Func<Node, Task> OnNodePopulatedNoAction = _ => Task.CompletedTask;
        private static readonly Func<string, Task> OnLinkDiscoveredNoAction = _ => Task.CompletedTask;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _webGraph = new InMemoryWebGraphAdapter(_logger.Object);
        }

        [Test]
        public async Task AddWebPageAsync_AddingPage_IncrementsTotalPopulatedNodes()
        {
            var page = new WebPageItem()
            {
                GraphId = 1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTime.UtcNow.AddMonths(-1),
                Links = new List<string>()
            };

            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);
            var total = await _webGraph.TotalPopulatedNodesAsync(page.GraphId);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_DifferentGraphIds_AreIsolated()
        {
            var page1 = new WebPageItem()
            {
                GraphId = 1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string>()
            };

            var page2 = new WebPageItem()
            {
                GraphId = 2,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string> { "B" }
            };

            await _webGraph.AddWebPageAsync(page1, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);
            await _webGraph.AddWebPageAsync(page2, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            var total1 = await _webGraph.TotalPopulatedNodesAsync(graphId: 1);
            var total2 = await _webGraph.TotalPopulatedNodesAsync(graphId: 2);

            Assert.That(total1, Is.EqualTo(1));
            Assert.That(total2, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_SelfLink_ShouldBeIgnored()
        {
            var page = new WebPageItem
            {
                GraphId = 1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "A" } // Self-link
            };

            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            var node = await _webGraph.GetNodeAsync(page.GraphId, "A");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.State, Is.EqualTo(NodeState.Populated));
            Assert.That(node.OutgoingLinks, Is.Empty, "Self-link should have been ignored");
        }


        [Test]
        public async Task AddWebPageAsync_SameUrl_UnchangedContent_NotAddedTwice()
        {
            var now = DateTimeOffset.UtcNow;

            var page = new WebPageItem
            {
                GraphId = 1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = now,
                Links = new List<string>()
            };

            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);
            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction); // same again

            var total = await _webGraph.TotalPopulatedNodesAsync(1);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_LinkIsAdded_TargetIsDummy()
        {
            var page = new WebPageItem
            {
                GraphId = 1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" }
            };

            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            // Retrieve both nodes
            var nodeA = await _webGraph.GetNodeAsync(1, "A");
            var nodeB = await _webGraph.GetNodeAsync(1, "B");

            // Confirm link from A to B
            Assert.That(nodeA.OutgoingLinks.Any(link => link.Url == "B"), Is.True, "Link from A to B should exist.");

            // Confirm B exists and is Dummy
            Assert.That(nodeB, Is.Not.Null, "Node B should have been created.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Dummy), "Node B should be in Dummy state.");
        }

        [Test]
        public async Task AddWebPageAsync_PageB_PromotedToPopulated()
        {
            int graphId = 1;

            // Add Page A -> B
            var pageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" }
            };
            await _webGraph.AddWebPageAsync(pageA, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            // Add Page B -> C
            var pageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "B",
                OriginalUrl = "B",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "C" }
            };
            await _webGraph.AddWebPageAsync(pageB, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            // Get node B and verify it's populated
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");

            Assert.That(nodeB, Is.Not.Null, "Node B should exist.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Populated), "Node B should be in Populated state.");
        }

        [Test]
        public async Task AddWebPageAsync_PageBRedirectsToC_RedirectBehaviorVerified()
        {
            int graphId = 1;

            // Step 1: Add Page A -> B
            var pageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" }
            };
            await _webGraph.AddWebPageAsync(pageA, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            // Step 2: Add Page B -> C (redirect)
            var pageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "C",               // final URL after redirect
                OriginalUrl = "B",       // B redirects to C
                IsRedirect = true,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string>()
            };
            await _webGraph.AddWebPageAsync(pageB, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            // ASSERT 1: Node B should exist and be in Redirected state
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");
            Assert.That(nodeB, Is.Not.Null);
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Redirected), "Node B should be Redirected");

            // ASSERT 2: Node A should still point to B
            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            Assert.That(nodeA.OutgoingLinks.Any(n => n.Url == "B"), "Node A should still link to B");

            // ASSERT 3: Node C should exist and be Populated
            var nodeC = await _webGraph.GetNodeAsync(graphId, "C");
            Assert.That(nodeC, Is.Not.Null);
            Assert.That(nodeC.State, Is.EqualTo(NodeState.Populated), "Node C should be Populated");

            // ASSERT 4: Node B should link to C
            Assert.That(nodeB.OutgoingLinks.Any(n => n.Url == "C"), "Node B should link to C as a redirect");

            // ASSERT 5 (optional): Node A does not directly link to C
            Assert.That(nodeA.OutgoingLinks.All(n => n.Url != "C"), "Node A should not link directly to C");

            // ASSERT 6 (optional): Total populated nodes = 2 (A and C)
            var total = await _webGraph.TotalPopulatedNodesAsync(graphId);
            Assert.That(total, Is.EqualTo(2), "Only A and C should be populated nodes");
        }

        [Test]
        public async Task AddWebPageAsync_RedirectFromAToB_ShouldCreateRedirectNodeA_AndPopulatedNodeB()
        {
            var page = new WebPageItem
            {
                GraphId = 1,
                OriginalUrl = "A",
                Url = "B",
                IsRedirect = true,
                SourceLastModified = DateTime.UtcNow,
                Links = new List<string> { "C" }
            };

            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, OnLinkDiscoveredNoAction);

            var nodeA = await _webGraph.GetNodeAsync(1, "A");
            var nodeB = await _webGraph.GetNodeAsync(1, "B");
            var nodeC = await _webGraph.GetNodeAsync(1, "C");

            Assert.Multiple(() =>
            {
                Assert.That(nodeA, Is.Not.Null, "Node A should exist");
                Assert.That(nodeA.State, Is.EqualTo(NodeState.Redirected), "Node A should be redirected");
                Assert.That(nodeA.OutgoingLinks.Any(l => l.Url == "B"), "Node A should link to B");

                Assert.That(nodeB, Is.Not.Null, "Node B should exist");
                Assert.That(nodeB.State, Is.EqualTo(NodeState.Populated), "Node B should be populated");
                Assert.That(nodeB.OutgoingLinks.Any(l => l.Url == "C"), "Node B should link to C");

                Assert.That(nodeC, Is.Not.Null, "Node C should exist");
                Assert.That(nodeC.State, Is.EqualTo(NodeState.Dummy), "Node C should be dummy");
            });
        }

        [Test]
        public async Task AddWebPageAsync_ShouldNotInvoke_OnLinkDiscovered_WhenAlreadyRecentlyScheduled()
        {
            // Arrange
            var graphId = 1;

            var mockSchedules = new List<string>();
            Func<string, Task> onLinkDiscovered = url =>
            {
                mockSchedules.Add(url);
                return Task.CompletedTask;
            };

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" }
            };

            // First call should schedule B
            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, onLinkDiscovered);

            // Immediately call again - should NOT re-schedule B due to throttling
            await _webGraph.AddWebPageAsync(page, OnNodePopulatedNoAction, onLinkDiscovered);

            Assert.That(mockSchedules.Count, Is.EqualTo(1), "Link B should only be scheduled once due to throttle");
        }


    }
}
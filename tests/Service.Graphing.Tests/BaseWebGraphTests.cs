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
        private static readonly Func<Node, Task> NodePopulatedCallbackNoAction = _ => Task.CompletedTask;
        private static readonly Func<Node, Task> LinkDiscoveredCallbackNoAction = _ => Task.CompletedTask;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _webGraph = new InMemoryWebGraphAdapter(_logger.Object);
        }

        [Test]
        public async Task AddWebPageAsync_AddingPage_IncrementsTotalPopulatedNodes()
        {
            var graphId = Guid.NewGuid();

            var page = new WebPageItem()
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTime.UtcNow.AddMonths(-1),
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);
            var total = await _webGraph.TotalPopulatedNodesAsync(page.GraphId);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_DifferentGraphIds_AreIsolated()
        {
            var graphId1 = Guid.NewGuid();
            var graphId2 = Guid.NewGuid();

            var page1 = new WebPageItem()
            {
                GraphId = graphId1,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            var page2 = new WebPageItem()
            {
                GraphId = graphId2,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page1, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);
            await _webGraph.AddWebPageAsync(page2, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            var total1 = await _webGraph.TotalPopulatedNodesAsync(graphId: graphId1);
            var total2 = await _webGraph.TotalPopulatedNodesAsync(graphId: graphId2);

            Assert.That(total1, Is.EqualTo(1));
            Assert.That(total2, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_SelfLink_ShouldBeIgnored()
        {
            var graphId = Guid.NewGuid();

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "A" }, // Self-link
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            var node = await _webGraph.GetNodeAsync(page.GraphId, "A");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.State, Is.EqualTo(NodeState.Populated));
            Assert.That(node.OutgoingLinks, Is.Empty, "Self-link should have been ignored");
        }


        [Test]
        public async Task AddWebPageAsync_SameUrl_UnchangedContent_NotAddedTwice()
        {
            var graphId = Guid.NewGuid();

            var now = DateTimeOffset.UtcNow;

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = now,
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);
            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction); // same again

            var total = await _webGraph.TotalPopulatedNodesAsync(graphId);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task AddWebPageAsync_LinkIsAdded_TargetIsDummy()
        {
            var graphId = Guid.NewGuid();

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            // Retrieve both nodes
            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");

            // Confirm link from A to B
            Assert.That(nodeA.OutgoingLinks.Any(link => link.Url == "B"), Is.True, "Link from A to B should exist.");

            // Confirm B exists and is Dummy
            Assert.That(nodeB, Is.Not.Null, "Node B should have been created.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Dummy), "Node B should be in Dummy state.");
        }

        [Test]
        public async Task AddWebPageAsync_PageB_PromotedToPopulated()
        {
            var graphId = Guid.NewGuid();

            // Add Page A -> B
            var pageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };
            await _webGraph.AddWebPageAsync(pageA, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            // Add Page B -> C
            var pageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "B",
                OriginalUrl = "B",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "C" },
                ContentFingerprint = ""
            };
            await _webGraph.AddWebPageAsync(pageB, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            // Get node B and verify it's populated
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");

            Assert.That(nodeB, Is.Not.Null, "Node B should exist.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Populated), "Node B should be in Populated state.");
        }

        [Test]
        public async Task AddWebPageAsync_PageBRedirectsToC_RedirectBehaviorVerified()
        {
            var graphId = Guid.NewGuid();

            // Step 1: Add Page A -> B
            var pageA = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };
            await _webGraph.AddWebPageAsync(pageA, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            // Step 2: Add Page B -> C (redirect)
            var pageB = new WebPageItem
            {
                GraphId = graphId,
                Url = "C",               // final URL after redirect
                OriginalUrl = "B",       // B redirects to C
                IsRedirect = true,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string>(),
                ContentFingerprint = ""
            };
            await _webGraph.AddWebPageAsync(pageB, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

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
            var graphId = Guid.NewGuid();

            var page = new WebPageItem
            {
                GraphId = graphId,
                OriginalUrl = "A",
                Url = "B",
                IsRedirect = true,
                SourceLastModified = DateTime.UtcNow,
                Links = new List<string> { "C" },
                ContentFingerprint = ""
            };

            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, LinkDiscoveredCallbackNoAction);

            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");
            var nodeC = await _webGraph.GetNodeAsync(graphId, "C");

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
            var graphId = Guid.NewGuid();

            var mockSchedules = new List<Node>();
            Func<Node, Task> onLinkDiscovered = node =>
            {
                mockSchedules.Add(node);
                return Task.CompletedTask;
            };

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            // First call should schedule B
            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, onLinkDiscovered);

            // Immediately call again - should NOT re-schedule B due to throttling
            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, onLinkDiscovered);

            Assert.That(mockSchedules.Count, Is.EqualTo(1), "Link B should only be scheduled once due to throttle");
        }

        [Test]
        public async Task AddWebPageAsync_ShouldInvoke_OnLinkDiscovered_WhenForceRefreshIsTrue()
        {
            var graphId = Guid.NewGuid();
            var mockSchedules = new List<Node>();
            Func<Node, Task> onLinkDiscovered = node =>
            {
                mockSchedules.Add(node);
                return Task.CompletedTask;
            };

            var page = new WebPageItem
            {
                GraphId = graphId,
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            // First call schedules B
            await _webGraph.AddWebPageAsync(page, forceRefresh: false, NodePopulatedCallbackNoAction, onLinkDiscovered);

            // Second call immediately after with forceRefresh: true
            await _webGraph.AddWebPageAsync(page, forceRefresh: true, NodePopulatedCallbackNoAction, onLinkDiscovered);

            Assert.That(mockSchedules.Count, Is.EqualTo(2), "Link B should be scheduled again due to forceRefresh override");
        }

    }
}
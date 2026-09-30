using System.Runtime;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Graphing.Infrastructure.WebGraph.Adapters.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Graphing.Core.Tests
{
    [TestFixture]
    public class BaseWebGraphTests
    {
        private Mock<ILogger> _logger;
        private BaseWebGraph _webGraph;
        private GraphingSettings _graphingSettings;

        // Most tests simulate a recursive crawl where the Node refresh throttle applies.
        // A crawl depth greater than 0 enables the throttle.
        private const int CrawlDepth = 1;

        private static readonly Func<Node, Task> NodePopulatedCallbackNoAction = _ => Task.CompletedTask;
        private static readonly Func<Node, Task> NodePopulationRequestCallbackNoAction = _ => Task.CompletedTask;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _graphingSettings = new GraphingSettings();
            _webGraph = new MemoryWebGraphAdapter(_logger.Object, _graphingSettings);
        }

        [Test]
        public async Task MapPageAsync_AddingPage_IncrementsTotalPopulatedNodes()
        {
            var graphId = Guid.NewGuid();

            var pageData = new PageData()
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTime.UtcNow.AddMonths(-1),
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            var total = await _webGraph.TotalPopulatedNodesAsync(graphId);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task MapPageAsync_DifferentGraphIds_AreIsolated()
        {
            var graphId1 = Guid.NewGuid();
            var graphId2 = Guid.NewGuid();

            var page1 = new PageData()
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            var page2 = new PageData()
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow.AddMonths(-1),
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId1, 
                page1,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            await _webGraph.MapPageAsync(
                graphId2, 
                page2,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            var total1 = await _webGraph.TotalPopulatedNodesAsync(graphId1);
            var total2 = await _webGraph.TotalPopulatedNodesAsync(graphId2);

            Assert.That(total1, Is.EqualTo(1));
            Assert.That(total2, Is.EqualTo(1));
        }

        [Test]
        public async Task MapPageAsync_SelfLink_ShouldBeIgnored()
        {
            var graphId = Guid.NewGuid();

            var pageData = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "A" }, // Self-link
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            var node = await _webGraph.GetNodeAsync(graphId, "A");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.State, Is.EqualTo(NodeState.Populated));
            Assert.That(node.OutgoingNodes, Is.Empty, "Self-link should have been ignored");
        }


        [Test]
        public async Task MapPageAsync_SameUrl_UnchangedContent_NotAddedTwice()
        {
            var graphId = Guid.NewGuid();

            var now = DateTimeOffset.UtcNow;

            var pageData = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = now,
                Links = new List<string>(),
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            await _webGraph.MapPageAsync(
                graphId, pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction); // same again

            var total = await _webGraph.TotalPopulatedNodesAsync(graphId);

            Assert.That(total, Is.EqualTo(1));
        }

        [Test]
        public async Task MapPageAsync_LinkIsAdded_TargetIsDummy()
        {
            var graphId = Guid.NewGuid();

            var pageData = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            // Retrieve both nodes
            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");

            // Confirm Node A has an outgoing relationship to Node B
            Assert.That(nodeA.OutgoingNodes.Any(link => link.Url == "B"), Is.True, "Node A should have an outgoing relationship to Node B.");

            // Confirm B exists and is Dummy
            Assert.That(nodeB, Is.Not.Null, "Node B should have been created.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Dummy), "Node B should be in Dummy state.");
        }

        [Test]
        public async Task MapPageAsync_PageB_PromotedToPopulated()
        {
            var graphId = Guid.NewGuid();

            // Map Page A with a link to B
            var pageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };
            await _webGraph.MapPageAsync(
                graphId, 
                pageA,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            // Map Page B with a link to C
            var pageB = new PageData
            {
                Url = "B",
                OriginalUrl = "B",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "C" },
                ContentFingerprint = ""
            };
            await _webGraph.MapPageAsync(
                graphId, 
                pageB,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            // Get node B and verify it's populated
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");

            Assert.That(nodeB, Is.Not.Null, "Node B should exist.");
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Populated), "Node B should be in Populated state.");
        }

        [Test]
        public async Task MapPageAsync_PageBRedirectsToC_RedirectBehaviorVerified()
        {
            var graphId = Guid.NewGuid();

            // Step 1: Map Page A with a link to B
            var pageA = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };
            await _webGraph.MapPageAsync(
                graphId, 
                pageA,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            // Step 2: Map redirected Page B to C
            var pageB = new PageData
            {
                Url = "C",               // final URL after redirect
                OriginalUrl = "B",       // B redirects to C
                IsRedirect = true,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string>(),
                ContentFingerprint = ""
            };
            await _webGraph.MapPageAsync(
                graphId, 
                pageB,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            // ASSERT 1: Node B should exist and be in Redirected state
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");
            Assert.That(nodeB, Is.Not.Null);
            Assert.That(nodeB.State, Is.EqualTo(NodeState.Redirected), "Node B should be Redirected");

            // ASSERT 2: Node A should still have an outgoing relationship to Node B
            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            Assert.That(nodeA.OutgoingNodes.Any(n => n.Url == "B"), "Node A should have an outgoing relationship to B");

            // ASSERT 3: Node C should exist and be Populated
            var nodeC = await _webGraph.GetNodeAsync(graphId, "C");
            Assert.That(nodeC, Is.Not.Null);
            Assert.That(nodeC.State, Is.EqualTo(NodeState.Populated), "Node C should be Populated");

            // ASSERT 4: Node B should have an outgoing relationship to Node C
            Assert.That(nodeB.OutgoingNodes.Any(n => n.Url == "C"), "Node B should have an outgoing relationship to C as a redirect");

            // ASSERT 5 (optional): Node A has no direct relationship to Node C
            Assert.That(nodeA.OutgoingNodes.All(n => n.Url != "C"), "Node A should not link directly to C");

            // ASSERT 6 (optional): Total populated nodes = 2 (A and C)
            var total = await _webGraph.TotalPopulatedNodesAsync(graphId);
            Assert.That(total, Is.EqualTo(2), "Only A and C should be populated nodes");
        }

        [Test]
        public async Task MapPageAsync_RedirectFromAToB_ShouldCreateRedirectNodeA_AndPopulatedNodeB()
        {
            var graphId = Guid.NewGuid();

            var pageData = new PageData
            {
                OriginalUrl = "A",
                Url = "B",
                IsRedirect = true,
                SourceLastModified = DateTime.UtcNow,
                Links = new List<string> { "C" },
                ContentFingerprint = ""
            };

            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                NodePopulationRequestCallbackNoAction);

            var nodeA = await _webGraph.GetNodeAsync(graphId, "A");
            var nodeB = await _webGraph.GetNodeAsync(graphId, "B");
            var nodeC = await _webGraph.GetNodeAsync(graphId, "C");

            Assert.Multiple(() =>
            {
                Assert.That(nodeA, Is.Not.Null, "Node A should exist");
                Assert.That(nodeA.State, Is.EqualTo(NodeState.Redirected), "Node A should be redirected");
                Assert.That(nodeA.OutgoingNodes.Any(l => l.Url == "B"), "Node A should have an outgoing relationship to B");

                Assert.That(nodeB, Is.Not.Null, "Node B should exist");
                Assert.That(nodeB.State, Is.EqualTo(NodeState.Populated), "Node B should be populated");
                Assert.That(nodeB.OutgoingNodes.Any(l => l.Url == "C"), "Node B should have an outgoing relationship to C");

                Assert.That(nodeC, Is.Not.Null, "Node C should exist");
                Assert.That(nodeC.State, Is.EqualTo(NodeState.Dummy), "Node C should be dummy");
            });
        }

        [Test]
        public async Task MapPageAsync_ExistingNodeWithinThrottle_DoesNotRequestPopulation()
        {
            // Arrange
            var graphId = Guid.NewGuid();

            var populationRequests = new List<Node>();
            Func<Node, Task> onNodePopulationRequestCallback = node =>
            {
                populationRequests.Add(node);
                return Task.CompletedTask;
            };

            var pageData = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            // First call requests population of B
            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                onNodePopulationRequestCallback);

            // Second call should not request population of B while it is throttled
            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                onNodePopulationRequestCallback);

            Assert.That(
                populationRequests.Count, 
                Is.EqualTo(1),
                "Node B should only be requested for population once while throttled.");
        }

        [Test]
        public async Task MapPageAsync_InitialCrawl_BypassesRefreshThrottle()
        {
            var graphId = Guid.NewGuid();

            var populationRequests = new List<Node>();
            Func<Node, Task> onNodePopulationRequestCallback = node =>
            {
                populationRequests.Add(node);
                return Task.CompletedTask;
            };

            var pageData = new PageData
            {
                Url = "A",
                OriginalUrl = "A",
                IsRedirect = false,
                SourceLastModified = DateTimeOffset.UtcNow,
                Links = new List<string> { "B" },
                ContentFingerprint = ""
            };

            // First call requests population of B
            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                CrawlDepth,
                NodePopulatedCallbackNoAction,
                onNodePopulationRequestCallback);

            // Initial crawl bypasses the refresh throttle
            await _webGraph.MapPageAsync(
                graphId, 
                pageData,
                crawlDepth: 0, // Throttle does not apply
                NodePopulatedCallbackNoAction,
                onNodePopulationRequestCallback);

            Assert.That(
                populationRequests.Count, 
                Is.EqualTo(2), 
                "Node B should be requested for population again during an initial crawl.");
        }

    }
}
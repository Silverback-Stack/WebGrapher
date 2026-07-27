using System;
using System.Net;
using System.Text;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Requests.Core;
using SitePolicy.Core;

namespace Scraper.Core.Tests
{
    [TestFixture]
    public class PageScraperTests
    {
        private Mock<ILogger> _logger;
        private Mock<IEventBus> _eventBus;
        private Mock<IRequestSender> _requestSender;
        private Mock<ISitePolicyResolver> _sitePolicyResolver;
        private IPageScraper _scraper;

        private const string UserAgent = "WebGrapher";
        private const string UserAccepts = "text/html";
        private const string GroupKey = "scraper-group";
        private const string CacheKey = "7f83b1657ff1fc53";
        private const string CacheContainer = "blob-cache";

        private Uri _url;
        private ScrapePageEvent _scrapePageEvent;
        private HttpResponseEnvelope _response;


        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _eventBus = new Mock<IEventBus>();
            _requestSender = new Mock<IRequestSender>();
            _sitePolicyResolver = new Mock<ISitePolicyResolver>();

            _url = new Uri("http://example.com/page.html");


            // Create a Crawl Page Request for the page to be scraped.
            var request = new CrawlPageRequestDto
            {
                Url = _url,
                RequestedAt = DateTimeOffset.UtcNow,
                Options = new CrawlPageRequestOptionsDto
                {
                    UserAgent = UserAgent,
                    UserAccepts = UserAccepts
                }
            };


            // Create a Scrape Page Event containing the request.
            _scrapePageEvent = new ScrapePageEvent {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow
            };


            // Create a successful response returned by the Request Sender.
            _response = new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = _url,
                    Url = _url,
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null,
                    ContentType = "text/html",
                    Encoding = "utf-8"
                },
                Data = new HttpResponseData
                {
                    Payload = Encoding.UTF8.GetBytes(
                        "<html><body>Example Page</body></html>")
                },
                Cache = new CacheInfo
                {
                    IsFromCache = false,
                    Key = CacheKey,
                    Container = CacheContainer
                },
                RequestSenderGroupKey = GroupKey
            };


            // Configure the Request Sender group used to resolve site policies.
            _requestSender
                .SetupGet(sender => sender.GroupKey)
                .Returns(GroupKey);


            // Configure the Request Sender to return the successful response.
            _requestSender
                .Setup(sender => sender.FetchAsync(
                    _url,
                    UserAgent,
                    UserAccepts,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_response);


            // Configure the Site Policy Resolver to indicate the site is not rate limited.
            _sitePolicyResolver
                .Setup(resolver => resolver.GetRateLimitAsync(
                    _url,
                    GroupKey))
                .ReturnsAsync((DateTimeOffset?)null);


            // Create the Page Scraper.
            _scraper = new PageScraper(
                _logger.Object,
                _eventBus.Object,
                _requestSender.Object,
                _sitePolicyResolver.Object,
                new ScraperSettings());
        }


        [Test]
        public async Task ScrapePageAsync_WhenSiteIsRateLimited_PublishesScrapePageFailedEvent()
        {
            // Arrange: Configure the Site Policy Resolver to indicate the site is rate limited.
            var limitedUntil = DateTimeOffset.UtcNow.AddMinutes(5);

            _sitePolicyResolver
                .Setup(resolver => resolver.GetRateLimitAsync(
                    _url,
                    GroupKey))
                .ReturnsAsync(limitedUntil);


            // Act: Scrape the webpage.
            await _scraper.ScrapePageAsync(_scrapePageEvent);


            // Assert: Verify a Scrape Page Failed event was published.
            _eventBus.Verify(bus => bus.PublishAsync(
                    It.Is<ScrapePageFailedEvent>(evt =>
                        evt.CrawlPageRequest == _scrapePageEvent.CrawlPageRequest &&
                        evt.StatusCode == HttpStatusCode.TooManyRequests &&
                        evt.RetryAfter == limitedUntil &&
                        evt.RequestSenderGroupKey == GroupKey),
                    It.IsAny<int>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Test]
        public async Task ScrapePageAsync_WithSuccessfulResponse_PublishesNormalisePageEvent()
        {
            // Act: Scrape the webpage.
            await _scraper.ScrapePageAsync(_scrapePageEvent);


            // Assert: Verify a Normalise Page event was published.
            _eventBus.Verify(bus => bus.PublishAsync(
                It.Is<NormalisePageEvent>(evt =>
                    evt.CrawlPageRequest == _scrapePageEvent.CrawlPageRequest &&
                    evt.ScrapePageResult.StatusCode == HttpStatusCode.OK &&
                    evt.ScrapePageResult.BlobId == CacheKey &&
                    evt.ScrapePageResult.BlobContainer == CacheContainer),
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }


        [Test]
        public async Task ScrapePageAsync_WhenRequestSenderReturnsNull_PublishesScrapePageFailedEvent()
        {
            // Arrange: Configure the Request Sender to return no response.
            _requestSender
                .Setup(sender => sender.FetchAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((HttpResponseEnvelope?)null);


            // Act: Scrape the webpage.
            await _scraper.ScrapePageAsync(_scrapePageEvent);


            // Assert: Verify a Scrape Page Failed event was published.
            _eventBus.Verify(bus => bus.PublishAsync(
                It.Is<ScrapePageFailedEvent>(evt =>
                    evt.CrawlPageRequest == _scrapePageEvent.CrawlPageRequest &&
                    evt.StatusCode == HttpStatusCode.ServiceUnavailable),
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }


        [Test]
        public async Task ScrapePageAsync_WithUnsuccessfulResponse_PublishesScrapePageFailedEvent()
        {
            // Arrange: Configure the response to indicate the remote service is unavailable.
            var failedResponse = _response with
            {
                Metadata = _response.Metadata with
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable
                }
            };

            _requestSender
                .Setup(sender => sender.FetchAsync(
                    _url,
                    UserAgent,
                    UserAccepts,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResponse);


            // Act: Scrape the webpage.
            await _scraper.ScrapePageAsync(_scrapePageEvent);


            // Assert: Verify a Scrape Page Failed event was published containing the response status code.
            _eventBus.Verify(bus => bus.PublishAsync(
                    It.Is<ScrapePageFailedEvent>(evt =>
                        evt.CrawlPageRequest == _scrapePageEvent.CrawlPageRequest &&
                        evt.StatusCode == HttpStatusCode.ServiceUnavailable),
                    It.IsAny<int>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

}

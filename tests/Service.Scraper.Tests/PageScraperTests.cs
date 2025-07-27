using System;
using System.Net;
using Events.Core.Bus;
using Logging.Core;
using Moq;
using Requests.Core;
using Scraper.Core;
using ScraperService;

namespace Service.Scraper.Tests
{
    [TestFixture]
    public class PageScraperTests
    {
        private Mock<ILogger> _logger;
        private Mock<IEventBus> _eventBus;
        private Mock<IRequestSender> _requestSender;
        private IScraper _scraper;

        private Uri _url;
        private string? _userAgent = null;
        private string? _userAccepts = null;

        [SetUp]
        public void Setup()
        {
            _logger= new Mock<ILogger>();
            _eventBus= new Mock<IEventBus>();
            _requestSender = new Mock<IRequestSender>();

            _url = new Uri("http://example.com/page.html");
            var contentMaxBytes = 0;

            var reponse = new ResponseEnvelope<ResponseItem>(
                new ResponseItem()
                {
                    OriginalUrl = _url,
                    RedirectedUrl = null,
                    Content = "<html></html>",
                    ContentType = "text/html",
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null
                },
                false);

            _requestSender.Setup(rs => rs.GetStringAsync(
                _url, _userAgent, _userAccepts, contentMaxBytes, CancellationToken.None))
                .ReturnsAsync(
                    new ResponseEnvelope<ResponseItem>(
                        new ResponseItem()
                        {
                            OriginalUrl = _url,
                            RedirectedUrl = null,
                            Content = "<html></html>",
                            ContentType = "text/html",
                            StatusCode = HttpStatusCode.OK,
                            Expires = DateTimeOffset.UtcNow.AddDays(1),
                            LastModified = DateTimeOffset.UtcNow,
                            RetryAfter = null
                        },
                        IsFromCache: false));

            _scraper = new PageScraper(_logger.Object, _eventBus.Object, _requestSender.Object);
        }

        [Test]
        public async Task GetAsync_WithResponseCode_ReturnsScrapeResponseItem()
        {
            var response = await _scraper.GetAsync(_url, _userAgent, _userAccepts);

            Assert.IsNotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

    }
}
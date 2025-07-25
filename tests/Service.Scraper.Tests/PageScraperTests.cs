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
        private string? _clientAccepts = null;

        [SetUp]
        public void Setup()
        {
            _logger= new Mock<ILogger>();
            _eventBus= new Mock<IEventBus>();
            _requestSender = new Mock<IRequestSender>();

            _url = new Uri("http://example.com/page.html");
            var contentMaxBytes = 0;

            _requestSender.Setup(rs => rs.GetStringAsync(
                _url, _userAgent, _clientAccepts, contentMaxBytes, CancellationToken.None))
                .ReturnsAsync(new RequestResponseItem()
                {
                    Content = "<html></html>",
                    StatusCode = HttpStatusCode.OK,
                    ContentType = "text/html",
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null
                });

            _scraper = new PageScraper(_logger.Object, _eventBus.Object, _requestSender.Object);
        }

        [Test]
        public async Task GetAsync_WithResponseCode_ReturnsScrapeResponseItem()
        {
            var response = await _scraper.GetAsync(_url, _userAgent, _clientAccepts);

            Assert.IsNotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

    }
}
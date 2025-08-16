using System;
using System.Net;
using System.Text;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Moq;
using Requests.Core;
using Scraper.Core;

namespace Service.Scraper.Tests
{
    [TestFixture]
    public class PageScraperTests
    {
        private Mock<ILogger> _logger;
        private Mock<IEventBus> _eventBus;
        private Mock<IRequestSender> _requestSender;
        private IPageScraper _scraper;

        private Uri _url;
        private string _userAgent = "";
        private string _userAccepts = "";

        [SetUp]
        public void Setup()
        {
            _logger= new Mock<ILogger>();
            _eventBus= new Mock<IEventBus>();
            _requestSender = new Mock<IRequestSender>();

            _url = new Uri("http://example.com/page.html");
            var contentMaxBytes = 0;

            //Mock http response:
            var responseEnvelope = new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = _url,
                    Url = null,
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null,
                    ResponseData = new HttpResponseDataItem { 
                        BlobId = "Blob1",
                        BlobContainer = "Blobs",
                        ContentType = "text/html", 
                        Encoding = "utf-8"
                    }
                },
                Data = new HttpResponseData {
                    Payload = Encoding.UTF8.GetBytes("<html><body>Example Page</body></html>")
                },
                IsFromCache = false
            };

            _requestSender.Setup(rs => rs.FetchAsync(
                _url, _userAgent, _userAccepts, contentMaxBytes, null, CancellationToken.None))
                .ReturnsAsync(responseEnvelope);

            _scraper = new PageScraper(_logger.Object, _eventBus.Object, _requestSender.Object);
        }

        [Test]
        public async Task GetAsync_WithResponseCode_ReturnsScrapeResponseItem()
        {
            var responseEnvelope = await _scraper.FetchAsync(_url, _userAgent, _userAccepts);

            Assert.IsNotNull(responseEnvelope);
            Assert.That(responseEnvelope.Metadata.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

    }
}
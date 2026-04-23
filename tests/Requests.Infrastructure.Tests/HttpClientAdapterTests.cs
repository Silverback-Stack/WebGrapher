using Requests.Infrastructure.Adapters.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Requests.Infrastructure.Tests
{
    [TestFixture]
    internal class HttpClientAdapterTests
    {

        /// <summary>
        /// Test HTTP handler that returns predefined responses instead of making real network requests.
        /// Used to isolate tests from external dependencies and avoid testing HttpClient itself.
        /// </summary>
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_responseFactory(request));
            }
        }

        [Test]
        public async Task GetAsync_WhenContentExceedsLimit_ReturnsTruncatedPayload()
        {
            var contentSizeBytes = 2_048_576;
            var contentMaxBytes = 1_048_576;

            var content = new string('X', contentSizeBytes);

            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, "text/html")
                };

                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
                return response;
            });

            var httpClient = new HttpClient(handler);
            var adapter = new HttpClientAdapter(
                httpClient,
                new HttpClientSettings());

            var result = await adapter.GetAsync(
                new Uri("http://example.com"),
                "test-agent",
                "text/html",
                contentMaxBytes);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Data!.Payload, Has.Length.EqualTo(contentMaxBytes));
        }


        [Test]
        public async Task GetAsync_WhenContentTypeIsNotAccepted_ReturnsNotAcceptable()
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<xml />", Encoding.UTF8, "application/xml")
                };

                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
                return response;
            });

            var httpClient = new HttpClient(handler);
            var adapter = new HttpClientAdapter(
                httpClient,
                new HttpClientSettings());

            var result = await adapter.GetAsync(
                new Uri("http://example.com"),
                "test-agent",
                "text/html");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Metadata.StatusCode, Is.EqualTo(HttpStatusCode.NotAcceptable));
        }

        [TestCase("*/*")]
        [TestCase("text/*")]
        public async Task GetAsync_WhenWildcardAcceptsContent_ReturnsOk(string acceptHeader)
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html />", Encoding.UTF8, "text/html")
                };

                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
                return response;
            });

            var httpClient = new HttpClient(handler);
            var adapter = new HttpClientAdapter(httpClient, new HttpClientSettings());

            var result = await adapter.GetAsync(
                new Uri("http://example.com"),
                "test-agent",
                acceptHeader);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Metadata.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }


        [Test]
        public async Task GetAsync_WhenRetryAfterHeaderIsMissingForTooManyRequests_ReturnsFallbackRetryAfter()
        {
            var settings = new HttpClientSettings
            {
                RetryAfterFallbackMinutes = 5
            };

            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent(string.Empty)
                };

                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
                return response;
            });

            var httpClient = new HttpClient(handler);
            var adapter = new HttpClientAdapter(httpClient, settings);

            var before = DateTimeOffset.UtcNow;

            var result = await adapter.GetAsync(
                new Uri("http://example.com"),
                "test-agent",
                "*/*");

            var after = DateTimeOffset.UtcNow;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Metadata.RetryAfter, Is.Not.Null);
            Assert.That(result.Metadata.RetryAfter, Is.GreaterThanOrEqualTo(before.AddMinutes(5)));
            Assert.That(result.Metadata.RetryAfter, Is.LessThanOrEqualTo(after.AddMinutes(5).AddSeconds(1)));
        }

    }

}

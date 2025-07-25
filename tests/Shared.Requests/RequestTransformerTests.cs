using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Requests.Core;

namespace Shared.Requests
{
    [TestFixture]
    public class RequestTransformerTests
    {

        [TestCase("text/plain", true)]
        [TestCase("text/html", true)]
        [TestCase("application/xml", true)]
        [TestCase("image/webp", true)]
        [TestCase("application/json", true)]

        public void IsContentAcceptable_WhenUsingAcceptHeader_ReturnsExpectedResult(
            string contentType, bool expected)
        {
            var clientAccepts = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";

            var result = RequestTransformer.IsContentAcceptable(contentType, clientAccepts);

            Assert.That(expected, Is.EqualTo(result));
        }

        [TestCase(HttpStatusCode.OK, true, false)]
        [TestCase(HttpStatusCode.OK, false, false)]
        [TestCase(HttpStatusCode.TooManyRequests, true, true)]
        [TestCase(HttpStatusCode.TooManyRequests, false, true)]
        [TestCase(HttpStatusCode.Forbidden, false, true)]
        [TestCase(HttpStatusCode.ServiceUnavailable, false, true)]
        public void GetRetryAfterOffset_WhenUsingStatusCodeAndRetryDate_ReturnsExpectedResult(
            HttpStatusCode statusCode, 
            bool retryAfterHasDate, 
            bool expected)
        {
            var retryAfter = new 
                RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(1));

            var results = RequestTransformer.GetRetryAfterOffset(
                statusCode, retryAfter);

            Assert.IsNotNull(results);
        }

        [Test]
        public async Task ReadAsStringAsync_ContentStreamLargerThanLimit_ReturnsTruncatedStream()
        {
            var contentSizeBytes = 2_048_576; //2 MB
            var contentMaxBytes = 1_048_576; //1 MB 

            HttpContent? content;
            content = new StringContent(
                new string('X', contentSizeBytes), Encoding.UTF8, "text/html");

            var result = await RequestTransformer.ReadAsStringAsync(content, contentMaxBytes);

            Assert.That(result, Has.Length.EqualTo(contentMaxBytes));
        }
    }
}

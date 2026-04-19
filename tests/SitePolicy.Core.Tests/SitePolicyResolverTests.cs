using Caching.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Requests.Core;
using System.Net;
using System.Text;

namespace SitePolicy.Core.Tests
{
    public class SitePolicyResolverTests
    {
        private Mock<ILogger> _logger = null!;
        private Mock<ICache> _policyCache = null!;
        private Mock<IRequestSender> _requestSender = null!;
        private ISitePolicyResolver _sitePolicyResolver = null!;

        private readonly Dictionary<string, object?> _cacheStore = new();

        private Uri _url;
        private string _userAgent = "TestBot";
        private string _partitionKey1 = "crawler-1";
        private string _partitionKey2 = "crawler-2";

        [SetUp]
        public void Setup()
        {
            // Clear shared in-memory cache between tests
            _cacheStore.Clear();

            // Create mocks for dependencies
            _logger = new Mock<ILogger>();
            _policyCache = new Mock<ICache>();
            _requestSender = new Mock<IRequestSender>();

            // Define test URL
            _url = new Uri("http://example.com/page.html");

            // Mock the RequestSender partition key (represents this crawler instance)
            _requestSender.SetupGet(x => x.PartitionKey)
                .Returns(_partitionKey1);

            // Mock cache GET for rate limit policy (reads from in-memory dictionary)
            _policyCache
                .Setup(x => x.GetAsync<SiteRateLimitPolicyItem>(It.IsAny<string>()))
                .ReturnsAsync((string key) =>
                {
                    if (_cacheStore.TryGetValue(key, out var value))
                        return value as SiteRateLimitPolicyItem;

                    return null;
                });

            // Mock cache GET for robots policy (reads from in-memory dictionary)
            _policyCache
                .Setup(x => x.GetAsync<SiteRobotsPolicyItem>(It.IsAny<string>()))
                .ReturnsAsync((string key) =>
                {
                    if (_cacheStore.TryGetValue(key, out var value))
                        return value as SiteRobotsPolicyItem;

                    return null;
                });

            // Mock cache SET for rate limit policy (writes to in-memory dictionary)
            _policyCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<SiteRateLimitPolicyItem>(), It.IsAny<TimeSpan>()))
                .Returns((string key, SiteRateLimitPolicyItem value, TimeSpan _) =>
                {
                    _cacheStore[key] = value;
                    return Task.CompletedTask;
                });

            // Mock cache SET for robots policy (writes to in-memory dictionary)
            _policyCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<SiteRobotsPolicyItem>(), It.IsAny<TimeSpan>()))
                .Returns((string key, SiteRobotsPolicyItem value, TimeSpan _) =>
                {
                    _cacheStore[key] = value;
                    return Task.CompletedTask;
                });

            // Configure site policy settings
            var settings = new SitePolicySettings
            {
                UserAccepts = "text/plain",
                AbsoluteExpiryMinutes = 20
            };

            // Create the resolver under test (using real implementation + mocked dependencies)
            _sitePolicyResolver = new SitePolicyResolver(
                _logger.Object,
                _policyCache.Object,
                _requestSender.Object,
                settings);
        }


        [Test]
        public async Task IsPermittedByRobotsTxtAsync_WhenPathIsDisallowed_ReturnsFalse()
        {
            // Arrange: define robots.txt that disallows /account
            var robotsUrl = new Uri("http://example.com/robots.txt");

            var robotsResponse = new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = robotsUrl,
                    Url = robotsUrl,
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null,
                    ContentType = "text/plain",
                    Encoding = "utf-8"
                },
                Data = new HttpResponseData
                {
                    Payload = Encoding.UTF8.GetBytes("User-agent: *\nDisallow: /account")
                },
                Cache = new CacheInfo
                {
                    IsFromCache = false,
                    Key = "robots",
                    Container = "Blobs",
                    PartitionKey = _partitionKey1
                }
            };

            _requestSender
                .Setup(x => x.FetchAsync(
                    It.Is<Uri>(u => u == robotsUrl),
                    _userAgent,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(robotsResponse);

            // Arrange: create a URL that should be denied by robots.txt
            var deniedUrl = new Uri("http://example.com/account");

            // Act: check robots.txt permission for the denied path
            var isPermitted = await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                deniedUrl,
                _userAgent);

            // Assert: crawling is NOT permitted for disallowed path
            Assert.That(isPermitted, Is.False);
        }


        [Test]
        public async Task IsPermittedByRobotsTxtAsync_WhenPathIsAllowed_ReturnsTrue()
        {
            // Arrange: define robots.txt that allows all paths
            var robotsUrl = new Uri("http://example.com/robots.txt");

            var robotsResponse = new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = robotsUrl,
                    Url = robotsUrl,
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null,
                    ContentType = "text/plain",
                    Encoding = "utf-8"
                },
                Data = new HttpResponseData
                {
                    Payload = Encoding.UTF8.GetBytes("User-agent: *\nAllow: /")
                },
                Cache = new CacheInfo
                {
                    IsFromCache = false,
                    Key = "robots",
                    Container = "Blobs",
                    PartitionKey = _partitionKey1
                }
            };

            _requestSender
                .Setup(x => x.FetchAsync(
                    It.Is<Uri>(u => u == robotsUrl),
                    _userAgent,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(robotsResponse);

            // Arrange: create a URL that should be allowed
            var allowedUrl = new Uri("http://example.com/home");

            // Act: check robots.txt permission for the allowed path
            var isPermitted = await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                allowedUrl,
                _userAgent);

            // Assert: crawling is permitted for allowed path
            Assert.That(isPermitted, Is.True);
        }


        [Test]
        public async Task IsPermittedByRobotsTxtAsync_WhenNoRobotsTxtFound_ReturnsTrue()
        {
            // Arrange: mock request sender to return null (robots.txt not found / request failed)
            var robotsUrl = new Uri("http://example.com/robots.txt");

            _requestSender
                .Setup(x => x.FetchAsync(
                    It.Is<Uri>(u => u == robotsUrl),
                    _userAgent,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((HttpResponseEnvelope?)null);

            // Act: check robots.txt permission for the page
            var isPermitted = await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                _url,
                _userAgent);

            // Assert: crawling is permitted when robots.txt is missing
            Assert.That(isPermitted, Is.True);
        }


        [Test]
        public async Task IsPermittedByRobotsTxtAsync_WhenFetchedByOneCrawlerInstance_IsSharedWithAnotherCrawlerInstance()
        {
            // Arrange: mock robots.txt response returned by the request sender
            var robotsUrl = new Uri("http://example.com/robots.txt");

            var robotsResponse = new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = robotsUrl,
                    Url = robotsUrl,
                    StatusCode = HttpStatusCode.OK,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    LastModified = DateTimeOffset.UtcNow,
                    RetryAfter = null,
                    ContentType = "text/plain",
                    Encoding = "utf-8"
                },
                Data = new HttpResponseData
                {
                    Payload = Encoding.UTF8.GetBytes("User-agent: *\nAllow: /")
                },
                Cache = new CacheInfo
                {
                    IsFromCache = false,
                    Key = "robots",
                    Container = "Blobs",
                    PartitionKey = _partitionKey1
                }
            };

            _requestSender
                .Setup(x => x.FetchAsync(
                    It.Is<Uri>(u => u == robotsUrl),
                    _userAgent,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(robotsResponse);

            // Act: crawler-1 checks robots.txt and causes it to be fetched and cached
            var crawler1Permitted = await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                _url,
                _userAgent);

            // Act: switch request sender to simulate crawler-2 using a different instance
            _requestSender.SetupGet(x => x.PartitionKey)
                .Returns(_partitionKey2);

            // Act: crawler-2 checks robots.txt and should use the cached shared robots policy
            var crawler2Permitted = await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                _url,
                _userAgent);

            // Assert: crawler-1 is permitted by robots.txt
            Assert.That(crawler1Permitted, Is.True);

            // Assert: crawler-2 is also permitted by the same shared robots policy
            Assert.That(crawler2Permitted, Is.True);

            // Assert: robots.txt was fetched only once because robots policy is shared
            _requestSender.Verify(x => x.FetchAsync(
                It.Is<Uri>(u => u == robotsUrl),
                _userAgent,
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Test]
        public async Task GetRateLimitAsync_WhenNoRateLimitExists_ReturnsNull()
        {
            // Act: retrieve rate limit when no policy has been stored
            var limitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey1);

            // Assert: no rate limit exists
            Assert.That(limitedUntil, Is.Null);
        }

        [Test]
        public async Task GetRateLimitAsync_WhenRateLimitIsExpired_ReturnsNull()
        {
            // Arrange: define an expired rate limit timestamp
            var expiredUntil = DateTimeOffset.UtcNow.AddMinutes(-5);

            // Arrange: set the expired rate limit for crawler-1
            await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                expiredUntil,
                _partitionKey1);

            // Act: retrieve rate limit for crawler-1
            var limitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey1);

            // Assert: expired rate limit is treated as not limited
            Assert.That(limitedUntil, Is.Null);
        }

        [Test]
        public async Task GetRateLimitAsync_WhenNoPartitionKeyProvided_UsesRequestSenderPartitionKey()
        {
            // Arrange: define a rate limit timestamp
            var limitedUntil = DateTimeOffset.UtcNow.AddMinutes(5);

            // Act: set rate limit WITHOUT providing partition key (uses default)
            await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                limitedUntil);

            // Act: retrieve rate limit WITHOUT providing partition key (uses same default)
            var result = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent);

            // Assert: rate limit is applied using RequestSender.PartitionKey
            Assert.That(result, Is.EqualTo(limitedUntil));
        }

        [Test]
        public async Task SetRateLimitedAsync_WhenSetForOneCrawlerInstance_DoesNotAffectAnotherCrawlerInstance()
        {
            // Arrange: define a rate limit timestamp
            var limitedUntil = DateTimeOffset.UtcNow.AddMinutes(5);

            // Act: set rate limit for crawler-1
            await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                limitedUntil,
                _partitionKey1);

            // Act: retrieve rate limit for crawler-1
            var crawler1LimitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey1);

            // Act: retrieve rate limit for crawler-2 (different cache partition)
            var crawler2LimitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey2);


            // Assert: crawler-1 is rate limited
            Assert.That(crawler1LimitedUntil, Is.EqualTo(limitedUntil));

            // Assert: crawler-2 is NOT rate limited,
            // therefore is not affected by another crawler instance
            Assert.That(crawler2LimitedUntil, Is.Null);
        }


        [Test]
        public async Task SetRateLimitedAsync_WhenNewLimitIsLater_KeepsLaterTimestamp()
        {
            // Arrange: define an earlier and later rate limit for the same crawler instance
            var earlierUntil = DateTimeOffset.UtcNow.AddMinutes(5);
            var laterUntil = DateTimeOffset.UtcNow.AddMinutes(10);

            // Act: set the earlier rate limit first
            await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                earlierUntil,
                _partitionKey1);

            // Act: set the later rate limit second
            var effectiveUntil = await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                laterUntil,
                _partitionKey1);

            // Act: read back the stored rate limit
            var storedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey1);

            // Assert: the later rate limit is kept after merge
            Assert.That(effectiveUntil, Is.EqualTo(laterUntil));

            // Assert: the stored rate limit is also the later timestamp
            Assert.That(storedUntil, Is.EqualTo(laterUntil));
        }


        [Test]
        public async Task SetRateLimitedAsync_WhenNewLimitIsEarlier_DoesNotReplaceLaterTimestamp()
        {
            // Arrange: define a later and earlier rate limit for the same crawler instance
            var laterUntil = DateTimeOffset.UtcNow.AddMinutes(10);
            var earlierUntil = DateTimeOffset.UtcNow.AddMinutes(5);

            // Act: set the later rate limit first
            await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                laterUntil,
                _partitionKey1);

            // Act: attempt to replace it with an earlier rate limit
            var effectiveUntil = await _sitePolicyResolver.SetRateLimitAsync(
                _url,
                _userAgent,
                earlierUntil,
                _partitionKey1);

            // Act: read back the stored rate limit
            var storedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                _url,
                _userAgent,
                _partitionKey1);

            // Assert: the earlier rate limit does NOT replace the later one
            Assert.That(effectiveUntil, Is.EqualTo(laterUntil));

            // Assert: the stored rate limit remains the later timestamp
            Assert.That(storedUntil, Is.EqualTo(laterUntil));
        }
    }
}
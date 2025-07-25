using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core.Policies
{
    public abstract class BaseSitePolicy 
    {
        protected readonly ILogger _logger;
        private readonly ICache _cache;
        protected readonly IRequestSender _requestSender;

        protected const int DEFAULT_ABSOLUTE_EXPIRY_DAYS = 7;

        protected BaseSitePolicy(ILogger logger, ICache cache, IRequestSender requestSender)
        {
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;
        }

        protected string CacheKey(Uri url) => url.Authority;

        protected async Task<SiteItem?> GetSiteItemAsync(Uri url)
        {
            var cacheKey = CacheKey(url);
            return await _cache.GetAsync<SiteItem>(cacheKey);
        }

        protected async Task SetSiteItemAsync(SiteItem item)
        {
            var cacheKey = CacheKey(item.Url);
            var expiryDuration = TimeSpan.FromDays(DEFAULT_ABSOLUTE_EXPIRY_DAYS);
            await _cache.SetAsync<SiteItem>(cacheKey, item, expiryDuration);
        }

        protected SiteItem NewSiteItem(
            Uri url, 
            HttpStatusCode statusCode, 
            DateTimeOffset? retryAfter = null,
            string robotsTxtContent = "")
        {
            return new SiteItem()
            {
                Url = url,
                StatusCode = statusCode,
                FetchedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(DEFAULT_ABSOLUTE_EXPIRY_DAYS),
                RobotsTxtContent = robotsTxtContent
            };
        }
    }
}

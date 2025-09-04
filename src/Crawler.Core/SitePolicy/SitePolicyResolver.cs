using System;
using Caching.Core;
using Caching.Core.Helpers;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core.SitePolicy
{
    public class SitePolicyResolver : ISitePolicyResolver
    {
        private readonly ILogger _logger;
        private readonly ICache _policyCache;
        private readonly IRequestSender _requestSender;
        private readonly CrawlerSettings _crawlerSettings;

        public SitePolicyResolver(ILogger logger, ICache policyCache, IRequestSender requestSender, CrawlerSettings crawlerSettings)
        {
            _logger = logger;
            _policyCache = policyCache;
            _requestSender = requestSender;
            _crawlerSettings = crawlerSettings;
        }

        public async Task<SitePolicyItem> GetOrCreateSitePolicyAsync(Uri url, string userAgent, DateTimeOffset? retryAfter = null)
        {
            var compositeKey = $"{url.Authority}|{userAgent}|{_crawlerSettings.SitePolicy.UserAccepts}";
            var cacheKey = CacheKeyHelper.ComputeCacheKey(compositeKey);

            var sitePolicy = await _policyCache.GetAsync<SitePolicyItem>(cacheKey);

            if (sitePolicy == null)
            {
                var robotsTxt = await FetchRobotsTxtAsync(url, userAgent);

                //check if HTML document
                if (robotsTxt?.Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogDebug($"Invalid Robots.txt detected for {url.AbsoluteUri} file detected as HTML.");
                    robotsTxt = null;
                }

                sitePolicy = new SitePolicyItem
                {
                    UrlAuthority = url.Authority,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_crawlerSettings.SitePolicy.AbsoluteExpiryMinutes),
                    RetryAfter = retryAfter,
                    RobotsTxt = robotsTxt ?? string.Empty
                };

                _logger.LogDebug($"Created site policy for {url.Authority}");
            }

            await SetSitePolicyAsync(url, userAgent, sitePolicy);

            return sitePolicy;
        }

        private async Task<string?> FetchRobotsTxtAsync(Uri url, string userAgent)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Host}/robots.txt");

            var httpResponseEnvelope = await _requestSender.FetchAsync(
                robotsTxtUrl,
                userAgent,
                _crawlerSettings.SitePolicy.UserAccepts);

            var encoding = httpResponseEnvelope?.Metadata?.ResponseData?.Encoding;
            return httpResponseEnvelope?.Data?.DecodeAsString(encoding);
        }

        /// <summary>
        /// PATTERN: Optermistic Concurrency with merge-on-write
        /// Rather than locking data as with pessimistic concurrency..
        /// Each service reads the latest version of the data, applies its changes and merges it with the latest version in the cache at write time
        /// </summary>
        private async Task SetSitePolicyAsync(Uri url, string userAgent, SitePolicyItem? sitePolicy)
        {
            if (sitePolicy is null) return;

            var compositeKey = $"{url.Authority}|{userAgent}|{_crawlerSettings.SitePolicy.UserAccepts}";
            var cacheKey = CacheKeyHelper.ComputeCacheKey(compositeKey);
            var expiryDuration = TimeSpan.FromMinutes(_crawlerSettings.SitePolicy.AbsoluteExpiryMinutes);

            var existingSitePolicy = await _policyCache.GetAsync<SitePolicyItem>(cacheKey);

            if (existingSitePolicy != null)
                sitePolicy = existingSitePolicy.MergePolicy(sitePolicy);

            _logger.LogDebug($"Saving policy for: {url.Authority}, expires at: {sitePolicy?.ExpiresAt.ToString("o")}");

            await _policyCache.SetAsync<SitePolicyItem>(cacheKey, sitePolicy!, expiryDuration);
        }

        public bool IsRateLimited(SitePolicyItem policy)
        {
            return policy.IsRateLimited;
        }

        public bool IsPermittedByRobotsTxt(Uri url, string userAgent, SitePolicyItem policy)
        {
            if (string.IsNullOrWhiteSpace(policy.RobotsTxt))
                return true;

            var robots = new RobotsTxt();
            robots.Load(policy.RobotsTxt);
            var isAllowed = robots.IsAllowed(userAgent, url.AbsolutePath);

            return isAllowed;
        }

    }
}

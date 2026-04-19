using Caching.Core;
using Caching.Core.Helpers;
using Microsoft.Extensions.Logging;
using Requests.Core;
using System;
using System.Net;
using Toimik.RobotsProtocol;

namespace SitePolicy.Core
{
    public class SitePolicyResolver : ISitePolicyResolver
    {
        private readonly ILogger _logger;
        private readonly ICache _policyCache;
        private readonly IRequestSender _requestSender;
        private readonly SitePolicySettings _sitePolicySettings;

        public SitePolicyResolver(
            ILogger logger, 
            ICache policyCache, 
            IRequestSender requestSender,
            SitePolicySettings sitePolicySettings)
        {
            _logger = logger;
            _policyCache = policyCache;
            _requestSender = requestSender;
            _sitePolicySettings = sitePolicySettings;
        }

        public async Task<bool> IsPermittedByRobotsTxtAsync(
            Uri url,
            string userAgent)
        {
            var robotsPolicy = await GetOrCreateRobotsPolicyAsync(url, userAgent);

            if (string.IsNullOrWhiteSpace(robotsPolicy.RobotsTxt))
                return true;

            var robots = new RobotsTxt();
            robots.Load(robotsPolicy.RobotsTxt);

            return robots.IsAllowed(userAgent, url.AbsolutePath);
        }


        public async Task<DateTimeOffset?> GetRateLimitAsync(
            Uri url,
            string userAgent,
            string? partitionKey = null)
        {
            var effectivePartitionKey = ResolvePartitionKey(partitionKey);

            var rateLimitPolicy = await GetRateLimitPolicyAsync(url, userAgent, effectivePartitionKey);

            return rateLimitPolicy?.IsRateLimited == true
                ? rateLimitPolicy.RetryAfter
                : null;
        }


        public async Task<DateTimeOffset?> SetRateLimitAsync(
            Uri url,
            string userAgent,
            DateTimeOffset until,
            string? partitionKey = null)
        {
            var effectivePartitionKey = ResolvePartitionKey(partitionKey);

            var rateLimitPolicy = new SiteRateLimitPolicyItem
            {
                UrlAuthority = url.Authority,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_sitePolicySettings.AbsoluteExpiryMinutes),
                RetryAfter = until
            };

            var savedPolicy = await SetRateLimitPolicyAsync(
                url,
                userAgent,
                rateLimitPolicy,
                effectivePartitionKey);

            return savedPolicy.RetryAfter;
        }


        private async Task<SiteRobotsPolicyItem> GetOrCreateRobotsPolicyAsync(
            Uri url,
            string userAgent)
        {
            var cacheKey = GetRobotsCacheKey(url, userAgent);

            var robotsPolicy = await _policyCache.GetAsync<SiteRobotsPolicyItem>(cacheKey);
            if (robotsPolicy is not null)
                return robotsPolicy;

            var robotsTxt = await FetchRobotsTxtAsync(url, userAgent);

            robotsPolicy = new SiteRobotsPolicyItem
            {
                UrlAuthority = url.Authority,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_sitePolicySettings.AbsoluteExpiryMinutes),
                RobotsTxt = robotsTxt ?? string.Empty
            };

            _logger.LogDebug($"Created robots policy for {url.Authority}");

            await _policyCache.SetAsync(
                cacheKey,
                robotsPolicy,
                TimeSpan.FromMinutes(_sitePolicySettings.AbsoluteExpiryMinutes));

            return robotsPolicy;
        }


        private async Task<SiteRateLimitPolicyItem?> GetRateLimitPolicyAsync(
            Uri url,
            string userAgent,
            string partitionKey)
        {
            var cacheKey = GetRateLimitCacheKey(url, userAgent, partitionKey);

            return await _policyCache.GetAsync<SiteRateLimitPolicyItem>(cacheKey);
        }


        /// <summary>
        /// PATTERN: Optimistic Concurrency with merge-on-write.
        /// Each service reads the latest version of the data, applies its changes,
        /// and merges it with the latest version in the cache at write time.
        /// </summary>
        private async Task<SiteRateLimitPolicyItem> SetRateLimitPolicyAsync(
            Uri url,
            string userAgent,
            SiteRateLimitPolicyItem rateLimitPolicy,
            string partitionKey)
        {
            var cacheKey = GetRateLimitCacheKey(url, userAgent, partitionKey);
            var expiryDuration = TimeSpan.FromMinutes(_sitePolicySettings.AbsoluteExpiryMinutes);

            var existingPolicy = await _policyCache.GetAsync<SiteRateLimitPolicyItem>(cacheKey);

            if (existingPolicy is not null)
                rateLimitPolicy = existingPolicy.Merge(rateLimitPolicy);

            _logger.LogDebug(
                $"Saving rate limit policy for: {url.Authority}, partition: {partitionKey}, until: {rateLimitPolicy.RetryAfter:o}");

            await _policyCache.SetAsync(cacheKey, rateLimitPolicy, expiryDuration);

            return rateLimitPolicy;
        }


        private async Task<string?> FetchRobotsTxtAsync(Uri url, string userAgent)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Authority}/robots.txt");

            var response = await _requestSender.FetchAsync(
                robotsTxtUrl,
                userAgent,
                _sitePolicySettings.UserAccepts);

            // Check if the request is rate limited
            var retryAfter = response?.Metadata.RetryAfter;
            if (retryAfter is not null)
            {
                await SetRateLimitAsync(
                    url,
                    userAgent,
                    retryAfter.Value,
                    _requestSender.PartitionKey);
            }

            string? robotsTxt = null;

            if (response?.Metadata.StatusCode == HttpStatusCode.OK)
            {
                var encoding = response.Metadata.Encoding;
                robotsTxt = response.Data?.DecodeAsString(encoding);

                var contentType = response.Metadata.ContentType ?? string.Empty;
                var looksLikeHtml =
                    robotsTxt?.Contains("<html", StringComparison.OrdinalIgnoreCase) == true &&
                    robotsTxt?.Contains("<body", StringComparison.OrdinalIgnoreCase) == true;

                if (looksLikeHtml)
                {
                    _logger.LogDebug($"Invalid Robots.txt detected for {url.AbsoluteUri}; file appears to be HTML.");
                    robotsTxt = null;
                }
            }

            return robotsTxt;
        }

        /// <summary>
        /// Resolve the effective partition key, 
        /// defaulting to the current RequestSender instance if none provided.
        /// </summary>
        private string ResolvePartitionKey(string? partitionKey)
        {
            return string.IsNullOrWhiteSpace(partitionKey)
                ? _requestSender.PartitionKey
                : partitionKey;
        }


        private string GetRobotsCacheKey(Uri url, string userAgent)
        {
            var compositeKey = $"{url.Authority}|{userAgent}|{_sitePolicySettings.UserAccepts}|robots";
            return CacheKeyHelper.ComputeCacheKey(compositeKey);
        }


        private string GetRateLimitCacheKey(
            Uri url,
            string userAgent,
            string partitionKey)
        {
            var compositeKey = $"{url.Authority}|{userAgent}|{_sitePolicySettings.UserAccepts}|ratelimit|{partitionKey}";

            return CacheKeyHelper.ComputeCacheKey(compositeKey);
        }

    }
}

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


        /// <summary>
        /// Determines whether a page is permitted to be crawled according
        /// to the site's robots.txt policy.
        /// </summary>
        public async Task<bool> IsPermittedByRobotsTxtAsync(
            Uri url,
            string userAgent)
        {
            var robotsPolicy = await GetOrCreateRobotsPolicyAsync(url, userAgent);

            // If no robots.txt content is available, allow crawling.
            if (string.IsNullOrWhiteSpace(robotsPolicy.RobotsTxt))
                return true;

            var robots = new RobotsTxt();
            robots.Load(robotsPolicy.RobotsTxt);

            return robots.IsAllowed(userAgent, url.AbsolutePath);
        }


        /// <summary>
        /// Returns the current Retry-After value if the site is rate limited;
        /// otherwise returns null.
        /// </summary>
        public async Task<DateTimeOffset?> GetRateLimitAsync(
            Uri url,
            string requestSenderGroupKey)
        {
            var effectiveGroupKey = ResolveRequestSenderGroupKey(requestSenderGroupKey);

            var rateLimitPolicy = await GetRateLimitPolicyAsync(url, effectiveGroupKey);

            // Return the Retry-After value, or null if the value has expired.
            return rateLimitPolicy?.IsRateLimited == true
                ? rateLimitPolicy.RetryAfter
                : null;
        }


        /// <summary>
        /// Updates the site's rate limiting policy and returns the
        /// effective Retry-After value after policy merging.
        /// </summary>
        public async Task<DateTimeOffset?> SetRateLimitAsync(
            Uri url,
            DateTimeOffset until,
            string requestSenderGroupKey)
        {
            var effectiveGroupKey = ResolveRequestSenderGroupKey(requestSenderGroupKey);

            var rateLimitPolicy = new SiteRateLimitPolicyItem
            {
                UrlAuthority = url.Authority,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_sitePolicySettings.PolicyExpiryMinutes),
                RetryAfter = until
            };

            var savedPolicy = await SetRateLimitPolicyAsync(
                url,
                rateLimitPolicy,
                effectiveGroupKey);

            // Return the effective Retry-After value after merging.
            return savedPolicy.RetryAfter;
        }



        /// <summary>
        /// Retrieves a site's robots policy from the Policy Store,
        /// creating and caching the policy if it does not already exist.
        /// </summary>
        private async Task<SiteRobotsPolicyItem> GetOrCreateRobotsPolicyAsync(
            Uri url,
            string userAgent)
        {
            // Get the cache key constrained to the default UserAccepts value.
            var cacheKey = GetRobotsCacheKey(
                url, 
                userAgent, 
                _sitePolicySettings.RobotsUserAccepts);

            // Return the existing policy if it has already been cached.
            var robotsPolicy = await _policyCache.GetAsync<SiteRobotsPolicyItem>(cacheKey);
            if (robotsPolicy is not null)
                return robotsPolicy;

            // Otherwise retrieve the site's robots.txt file and create a new policy.
            var robotsTxt = await FetchRobotsTxtAsync(url, userAgent);

            robotsPolicy = new SiteRobotsPolicyItem
            {
                UrlAuthority = url.Authority,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_sitePolicySettings.PolicyExpiryMinutes),
                RobotsTxt = robotsTxt ?? string.Empty
            };

            _logger.LogDebug("Created robots policy for {Authority}", url.Authority);

            // Store the newly created policy for future requests.
            await _policyCache.SetAsync(
                cacheKey,
                robotsPolicy,
                TimeSpan.FromMinutes(_sitePolicySettings.PolicyExpiryMinutes));

            return robotsPolicy;
        }


        /// <summary>
        /// Retrieves a site's robots.txt file and updates the site's
        /// rate limiting policy if the request is rate limited.
        /// </summary>
        private async Task<string?> FetchRobotsTxtAsync(Uri url, string userAgent)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Authority}/robots.txt");

            // Fetch the robots.txt file and constrain the Accept header
            // to content types supported by robots.txt processing.
            var response = await _requestSender.FetchAsync(
                robotsTxtUrl,
                userAgent,
                _sitePolicySettings.RobotsUserAccepts);

            // If the request was rate limited,
            // update the site's rate limiting policy.
            var retryAfter = response?.Metadata.RetryAfter;
            if (retryAfter is not null)
            {
                await SetRateLimitAsync(
                    url,
                    retryAfter.Value,
                    _requestSender.GroupKey);
            }

            return ExtractRobotsTxt(response, url);
        }


        /// <summary>
        /// Extracts and validates robots.txt content from a response.
        /// </summary>
        private string? ExtractRobotsTxt(HttpResponseEnvelope? response, Uri url)
        {
            if (response?.Metadata.StatusCode != HttpStatusCode.OK)
                return null;

            var robotsTxt = response.Data?.DecodeAsString(response.Metadata.Encoding);

            // Some websites incorrectly return HTML pages instead of robots.txt,
            // such as error pages or custom 404 responses.
            var looksLikeHtml =
                robotsTxt?.Contains("<html", StringComparison.OrdinalIgnoreCase) == true &&
                robotsTxt?.Contains("<body", StringComparison.OrdinalIgnoreCase) == true;

            if (!looksLikeHtml)
                return robotsTxt;

            _logger.LogDebug(
                "Invalid Robots.txt detected for {Url}; file appears to be HTML.",
                url.AbsoluteUri);

            return null;
        }


        /// <summary>
        /// Creates a cache key used to store and retrieve robots policies.
        /// Policies are partitioned by site authority, user agent,
        /// and request characteristics.
        /// </summary>
        private string GetRobotsCacheKey(
            Uri url,
            string userAgent,
            string userAccepts)
        {
            var compositeKey = $"{url.Authority}|{userAgent}|{userAccepts}|robots";

            return CacheKeyHelper.ComputeCacheKey(compositeKey);
        }



        /// <summary>
        /// Retrieves the rate limiting policy associated with the specified
        /// site and request sender group.
        /// </summary>
        private async Task<SiteRateLimitPolicyItem?> GetRateLimitPolicyAsync(
            Uri url,
            string requestSenderGroupKey)
        {
            var cacheKey = GetRateLimitCacheKey(
                url, 
                requestSenderGroupKey);

            return await _policyCache.GetAsync<SiteRateLimitPolicyItem>(cacheKey);
        }


        /// <summary>
        /// Saves a rate limiting policy using the Optimistic Concurrency Pattern
        /// to merge concurrent updates.
        /// </summary>
        private async Task<SiteRateLimitPolicyItem> SetRateLimitPolicyAsync(
            Uri url,
            SiteRateLimitPolicyItem rateLimitPolicy,
            string requestSenderGroupKey)
        {
            var cacheKey = GetRateLimitCacheKey(url, requestSenderGroupKey);
            var expiryDuration = TimeSpan.FromMinutes(_sitePolicySettings.PolicyExpiryMinutes);

            var existingPolicy = await _policyCache.GetAsync<SiteRateLimitPolicyItem>(cacheKey);

            // Merge with the latest policy to resolve concurrent updates.
            if (existingPolicy is not null)
                rateLimitPolicy = existingPolicy.Merge(rateLimitPolicy);

            _logger.LogDebug(
                "Saving rate limit policy for: {Authority}, group key: {RequestSenderGroupKey}, until: {RetryAfter}",
                url.Authority, requestSenderGroupKey, rateLimitPolicy.RetryAfter);

            await _policyCache.SetAsync(cacheKey, rateLimitPolicy, expiryDuration);

            return rateLimitPolicy;
        }


        /// <summary>
        /// Creates a cache key used to store and retrieve rate limiting policies.
        /// Policies are partitioned by site authority, and Request Sender Group Key.
        /// </summary>
        private string GetRateLimitCacheKey(
            Uri url,
            string requestSenderGroupKey)
        {
            var compositeKey = $"{url.Authority}|ratelimit|{requestSenderGroupKey}";

            return CacheKeyHelper.ComputeCacheKey(compositeKey);
        }



        /// <summary>
        /// Resolves the effective Request Sender Group Key,
        /// defaulting to the current Request Sender instance if none is provided.
        /// </summary>
        private string ResolveRequestSenderGroupKey(string requestSenderGroupKey)
        {
            return string.IsNullOrWhiteSpace(requestSenderGroupKey)
                ? _requestSender.GroupKey
                : requestSenderGroupKey;
        }


    }
}

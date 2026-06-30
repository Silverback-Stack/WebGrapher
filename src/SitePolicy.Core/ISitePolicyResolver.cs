namespace SitePolicy.Core
{
    public interface ISitePolicyResolver
    {
        /// <summary>
        /// Determines whether a page is permitted by the site's robots.txt policy.
        /// </summary>
        Task<bool> IsPermittedByRobotsTxtAsync(
            Uri url,
            string userAgent);

        /// <summary>
        /// Returns the current Retry-After value for a site's rate limiting policy,
        /// or null if requests are not currently rate limited.
        /// </summary>
        Task<DateTimeOffset?> GetRateLimitAsync(
            Uri url,
            string requestSenderGroupKey);

        /// <summary>
        /// Updates a site's rate limiting policy and returns the
        /// effective Retry-After value.
        /// </summary>
        Task<DateTimeOffset?> SetRateLimitAsync(
            Uri url,
            DateTimeOffset until,
            string requestSenderGroupKey);
    }
}
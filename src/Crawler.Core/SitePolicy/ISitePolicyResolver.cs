namespace Crawler.Core.SitePolicy
{
    public interface ISitePolicyResolver
    {
        Task<SitePolicyItem> GetOrCreateSitePolicyAsync(Uri url, string userAgent, DateTimeOffset? retryAfter = null);

        bool IsRateLimited(SitePolicyItem sitePolicyItem);
        bool IsPermittedByRobotsTxt(Uri url, string userAgent, SitePolicyItem sitePolicyItem);

    }
}
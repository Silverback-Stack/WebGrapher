
namespace Crawler.Core
{
    public interface ISitePolicyResolver
    {
        bool IsRateLimited(SitePolicyItem sitePolicyItem);
        Task<bool> IsPermittedByRobotsTxt(Uri url, string? userAgent, string? userAccepts, SitePolicyItem sitePolicyItem);
    }
}

namespace Crawler.Core
{
    public interface ISitePolicyResolver
    {
        bool IsRateLimited(SitePolicyItem sitePolicyItem);
        bool IsPermittedByRobotsTxt(Uri url, string? userAgent, SitePolicyItem sitePolicyItem);
        Task<string?> GetRobotsTxtContentAsync(Uri url, string? userAgent, string? userAccepts);
    }
}
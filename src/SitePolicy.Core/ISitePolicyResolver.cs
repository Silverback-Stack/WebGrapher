namespace SitePolicy.Core
{
    public interface ISitePolicyResolver
    {
        Task<bool> IsPermittedByRobotsTxtAsync(
            Uri url,
            string userAgent);

        Task<DateTimeOffset?> GetRateLimitAsync(
            Uri url,
            string userAgent,
            string? partitionKey = null);

        Task<DateTimeOffset?> SetRateLimitAsync(
            Uri url,
            string userAgent,
            DateTimeOffset until,
            string? partitionKey = null);
    }
}
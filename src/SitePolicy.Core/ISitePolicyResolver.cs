namespace SitePolicy.Core
{
    public interface ISitePolicyResolver
    {
        Task<bool> IsPermittedByRobotsTxtAsync(
            Uri url,
            string userAgent);

        Task<DateTimeOffset?> GetRateLimitAsync(
            Uri url,
            string requestSenderGroupKey);

        Task<DateTimeOffset?> SetRateLimitAsync(
            Uri url,
            DateTimeOffset until,
            string requestSenderGroupKey);
    }
}
using System;
using System.Net;
using System.Text;
using Logging.Core;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core
{
    public class SitePolicyResolver : ISitePolicyResolver
    {
        private readonly ILogger _logger;
        private readonly IRequestSender _requestSender;

        public SitePolicyResolver(ILogger logger, IRequestSender requestSender)
        {
            _logger = logger;
            _requestSender = requestSender;
        }

        public bool IsRateLimited(SitePolicyItem sitePolicyItem)
        {
            return sitePolicyItem.RetryAfter is not null && 
                DateTimeOffset.UtcNow < sitePolicyItem.RetryAfter;
        }

        public async Task<bool> IsPermittedByRobotsTxt(Uri url, string? userAgent, string? userAccepts, SitePolicyItem sitePolicyItem)
        {
            if (sitePolicyItem.RobotsTxtContent == null)
                sitePolicyItem = await GetRobotsTxtAsync(url, userAgent, userAccepts, sitePolicyItem);

            return IsPermittedByRobotsTxt(userAgent, url, sitePolicyItem);
        }

        private async Task<SitePolicyItem> GetRobotsTxtAsync(Uri url, string? userAgent, string? userAccepts, SitePolicyItem sitePolicyItem)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Host}/robots.txt");

            var response = await _requestSender.GetStringAsync(
                robotsTxtUrl,
                userAgent,
                userAccepts);

            sitePolicyItem.RobotsTxtContent = response?.Content;

            return sitePolicyItem;
        }

        private bool IsPermittedByRobotsTxt(string? userAgent, Uri url, SitePolicyItem sitePolicyItem)
        {
            if (string.IsNullOrWhiteSpace(sitePolicyItem.RobotsTxtContent))
                return true;

            if (userAgent == null) userAgent = string.Empty;

            var robots = new RobotsTxt();
            robots.Load(sitePolicyItem.RobotsTxtContent);

            var isAllowed = robots.IsAllowed(userAgent, url.AbsolutePath);

            if (!isAllowed)
                _logger.LogInformation($"Policy denied: RobotsTxt policy denied access to {url} for agent {userAgent}");

            return isAllowed;
        }
    }
}

using System;
using System.Net;
using System.Text;
using Caching.Core;
using Logging.Core;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core.Policies
{
    public class RobotsPolicy : BaseSitePolicy, IRobotsPolicy
    {
        private const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        private const string DEFAULT_CLIENT_ACCEPTS = "text/html";
        private const int DEFAULT_CONTENT_MAX_BYTES = 1_048_576; //1Mb

        public RobotsPolicy(
            ILogger logger,
            ICache cache,
            IRequestSender requestSender) : base(logger, cache, requestSender) { }

        public async Task<bool> IsAllowedAsync(Uri url, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = DEFAULT_USER_AGENT;

            var siteItem = await GetSiteItemAsync(url) ?? 
                await GetSiteItemAsync(url, userAgent);

            if (siteItem == null)
                return false;

            await SetSiteItemAsync(siteItem);

            return IsAllowed(userAgent, url, siteItem);
        }

        private async Task<SiteItem?> GetSiteItemAsync(Uri url, string? userAgent)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Host}/robots.txt");

            var response = await _requestSender.GetStringAsync(
                robotsTxtUrl, 
                userAgent, 
                DEFAULT_CLIENT_ACCEPTS, 
                DEFAULT_CONTENT_MAX_BYTES);

            if (response != null)
                return NewSiteItem(
                        url,
                        response?.StatusCode ?? HttpStatusCode.NotFound,
                        response?.RetryAfter,
                        response?.Content ?? string.Empty);

            return null;
        }

        private bool IsAllowed(string userAgent, Uri url, SiteItem siteItem)
        {
            if (siteItem.StatusCode != HttpStatusCode.OK)
                return true;
           
            var robots = new RobotsTxt();
            robots.Load(siteItem.RobotsTxtContent);

            var isAllowed = robots.IsAllowed(userAgent, url.AbsolutePath);

            if (!isAllowed)
                _logger.LogInformation($"Policy denied: RobotsTxt policy denied access to {url}");

            return isAllowed;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Logging.Core;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core.Robots
{
    public class RobotsService : IRobotsService
    {
        private readonly ILogger _logger;
        private readonly ICache _cache;
        private readonly IRequestSender _requestSender;

        private const int DEFAULT_ABSOLUTE_EXPIRY_DAYS = 30;

        public RobotsService(ILogger logger, ICache cache, IRequestSender requestSender)
        {
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;
        }

        public async Task<bool> IsUrlAllowedAsync(Uri url, string userAgent = "*", CancellationToken cancellationToken = default)
        {
            var robotsTxtUri = new Uri($"{url.Scheme}://{url.Host}/robots.txt");
            var robotsItem = _cache.Get<RobotsItem>(robotsTxtUri.AbsoluteUri);
            
            if (robotsItem == null || robotsItem.ExpiresAt < DateTimeOffset.UtcNow)
            {
                var response = await _requestSender.GetStringAsync(robotsTxtUri, cancellationToken);

                if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    robotsItem = new RobotsItem()
                    {
                        Url = robotsTxtUri,
                        FetchedAt = DateTimeOffset.UtcNow,
                        ExpiresAt = DateTimeOffset.UtcNow.AddDays(DEFAULT_ABSOLUTE_EXPIRY_DAYS),
                        RobotsTxtContent = response.Content ?? string.Empty,
                    };
                    _cache.Set<RobotsItem>(robotsTxtUri.AbsoluteUri, robotsItem, robotsItem.ExpiresAt - DateTimeOffset.UtcNow);
                }
            }

            if (robotsItem != null)
            {
                var robots = new RobotsTxt();
                robots.Load(robotsItem.RobotsTxtContent);
                return robots.IsAllowed(userAgent, url.PathAndQuery);
            }

            _logger.LogWarning($"Unable to parse robots.txt for {url}");
            return false;
        }

    }
}

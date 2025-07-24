using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core.Policies
{
    public class RateLimitPolicy : BaseSitePolicy, IRateLimitPolicy
    {
        public RateLimitPolicy(
            ILogger logger, 
            ICache cache, 
            IRequestSender requestSender) : base(logger, cache, requestSender) { }

        /// <summary>
        /// Returns the Retry After offset when the site is rate limiting requests.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool IsRateLimited(Uri url, out DateTimeOffset? retryAfter)
        {
            var siteItem = GetSiteItem(url);

            retryAfter = siteItem?.RetryAfter ?? null;

            if (siteItem == null) return false;

            if (siteItem.StatusCode == HttpStatusCode.TooManyRequests && 
                DateTimeOffset.UtcNow > siteItem.RetryAfter)
                    return false;

            return siteItem.StatusCode == HttpStatusCode.TooManyRequests;
        }

    }
}

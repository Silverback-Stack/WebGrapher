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
        public async Task<RateLimitResult> IsRateLimitedAsync(Uri url)
        {
            var result = new RateLimitResult()
            {
                IsRateLimited = false,
                RetryAfter = null
            };

            var siteItem = await GetSiteItemAsync(url);
            result.RetryAfter = siteItem?.RetryAfter;

            if (siteItem == null ||
                siteItem.StatusCode is HttpStatusCode.OK)
                return result;

            if (result.RetryAfter.HasValue &&
                DateTimeOffset.UtcNow > result.RetryAfter.Value)
                return result;

            if (!siteItem.RetryAfter.HasValue && 
                (siteItem.StatusCode is HttpStatusCode.TooManyRequests
                    or HttpStatusCode.Forbidden
                    or HttpStatusCode.ServiceUnavailable))
            {
                result.IsRateLimited = true;
                return result;
            }

            return result;
        }

    }

}

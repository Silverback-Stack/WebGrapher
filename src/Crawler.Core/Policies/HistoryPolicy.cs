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
    public class HistoryPolicy : BaseSitePolicy, IHistoryPolicy
    {
        public HistoryPolicy(
            ILogger logger,
            ICache cache,
            IRequestSender requestSender) : base(logger, cache, requestSender) { }

        public async Task SetResponseStatus(Uri url, HttpStatusCode statusCode, DateTimeOffset? retryAfter)
        {
            var siteItem = await GetSiteItemAsync(url);

            if (siteItem == null)
                siteItem = NewSiteItem(
                    url,
                    statusCode,
                    retryAfter);

            await SetSiteItemAsync(siteItem);
        }
    }
}

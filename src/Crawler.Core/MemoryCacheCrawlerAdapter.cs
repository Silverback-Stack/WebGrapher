using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Crawler.Core.RobotsEvaluator;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public class MemoryCacheCrawlerAdapter : BaseCrawler
    {
        public MemoryCacheCrawlerAdapter(
            IAppLogger logger, 
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender,
            IRobotsEvaluator robotsEvaluator) : base(logger, eventBus, cache, requestSender, robotsEvaluator) {
        }

        protected override Task<bool> GetHistory(Uri url)
        {
            throw new NotImplementedException();
        }

        protected override Task SetHistory(Uri url)
        {
            throw new NotImplementedException();
        }

        //internal override DateTimeOffset? GetUrl(string key)
        //{
        //    return _cache.Get<DateTimeOffset?>(key);
        //}

        //internal override void SetUrl(string key, DateTimeOffset value)
        //{
        //    _cache.Set(key, value, TimeSpan.FromMinutes(DEFAULT_ABSOLUTE_EXPIRY_MINUTES));
        //}
    }
}

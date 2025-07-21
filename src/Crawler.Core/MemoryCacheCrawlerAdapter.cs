using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public class MemoryCacheCrawlerAdapter : BaseCrawler
    {
        private readonly ILogger _logger;
        private readonly ICache _cache;
        private readonly IRequestSender _requestSender;

        public MemoryCacheCrawlerAdapter(IEventBus eventBus) : base(eventBus) {
            _logger = LoggingFactory.Create(LoggingOptions.File, nameof(MemoryCacheCrawlerAdapter));
            _cache = new MemoryCacheAdapter();
            _requestSender = new HttpClientRequestSender(_logger);
        }

        internal override DateTimeOffset? GetUrl(string key)
        {
            return _cache.Get<DateTimeOffset?>(key);
        }

        internal override void SetUrl(string key, DateTimeOffset value)
        {
            _cache.Set(key, value, TimeSpan.FromMinutes(DEFAULT_ABSOLUTE_EXPIRY_MINUTES));
        }
    }
}

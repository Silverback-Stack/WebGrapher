using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;

namespace Crawler.Core
{
    public class CrawlerFactory
    {
        public static ICrawler Create(CrawlerOptions storeType, IEventBus eventBus)
        {
            switch (storeType)
            {
                case CrawlerOptions.Memory:
                    return new MemoryCacheCrawlerAdapter(eventBus);

                case CrawlerOptions.LiteDb:
                    return new LiteDbCrawlerAdapter(eventBus);

                default:
                    throw new ArgumentOutOfRangeException(nameof(storeType),
                        $"Unsupported store type: {storeType}");
            }
        }
    }
}

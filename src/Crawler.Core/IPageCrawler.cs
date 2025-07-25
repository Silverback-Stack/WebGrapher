
using Events.Core.Bus;
using Events.Core.EventTypes;

namespace Crawler.Core
{
    public interface IPageCrawler
    {
        Task CrawlPage(CrawlPageEvent evt);
    }
}
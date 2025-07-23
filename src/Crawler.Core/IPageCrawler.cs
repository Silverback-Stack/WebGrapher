
using Events.Core.Bus;
using Events.Core.EventTypes;

namespace Crawler.Core
{
    public interface IPageCrawler
    {
        void Start();
        Task CrawlPage(CrawlPageEvent evt);
    }
}
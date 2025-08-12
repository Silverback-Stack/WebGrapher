using Events.Core.Events;

namespace Crawler.Core
{
    public interface IPageCrawler
    {
        Task EvaluatePageForCrawling(CrawlPageEvent evt);
    }
}
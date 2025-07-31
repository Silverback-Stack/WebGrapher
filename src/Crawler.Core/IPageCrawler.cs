using Events.Core.EventTypes;

namespace Crawler.Core
{
    public interface IPageCrawler
    {
        Task EvaluatePageForCrawling(CrawlPageEvent evt);
    }
}
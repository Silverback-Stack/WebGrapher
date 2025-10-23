using Events.Core.Events;

namespace Crawler.Core
{
    public interface IPageCrawler
    {
        Task StartAsync();
        Task StopAsync();
        Task EvaluatePageForCrawling(CrawlPageEvent evt);
    }
}
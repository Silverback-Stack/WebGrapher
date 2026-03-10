
namespace Crawler.Core
{
    public interface IPageCrawler
    {
        Task StartAsync();
        Task StopAsync();
    }
}
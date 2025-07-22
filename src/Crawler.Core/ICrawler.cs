
using Events.Core.Bus;
using Events.Core.Types;

namespace Crawler.Core
{
    public interface ICrawler : IEventBusLifecycle
    {
        Task CrawlPage(CrawlPageEvent evt);
    }
}
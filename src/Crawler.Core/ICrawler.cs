
using Events.Core.Bus;
using Events.Core.Types;

namespace Crawler.Core
{
    public interface ICrawler
    {
        void Start();
        Task CrawlPage(CrawlPageEvent evt);
    }
}
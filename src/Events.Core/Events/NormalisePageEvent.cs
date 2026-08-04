using System.Net;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record NormalisePageEvent
    {
        public DateTimeOffset CreatedAt { get; init; }

        public required CrawlPageRequestDto CrawlPageRequest { get; init; }
        
        public required ScrapePageResultDto ScrapePageResult { get; init; }
    }
}

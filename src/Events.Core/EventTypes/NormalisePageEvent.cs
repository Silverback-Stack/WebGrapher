using System.Net;
using Events.Core.Dtos;

namespace Events.Core.EventTypes
{
    public record NormalisePageEvent
    {
        public required CrawlPageRequestDto CrawlPageRequest { get; init; }
        
        public required ScrapePageResultDto ScrapePageResult { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}

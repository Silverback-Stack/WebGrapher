using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record GraphPageEvent
    {
        public required CrawlPageRequestDto CrawlPageRequest { get; init; }

        public required NormalisePageResultDto NormalisePageResult { get; init; }   

        public DateTimeOffset CreatedAt { get; init; }
    }
}

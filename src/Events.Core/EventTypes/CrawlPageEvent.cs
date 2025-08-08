using System;
using Events.Core.Dtos;

namespace Events.Core.EventTypes
{
    public record CrawlPageEvent
    {
        public CrawlPageRequestDto CrawlPageRequest { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        private CrawlPageEvent() { }

        private CrawlPageEvent(
            Uri url,
            int graphId,
            Guid correlationId,
            int attempt,
            int depth,
            int maxDepth,
            bool followExternalLinks,
            bool removeQueryStrings,
            IEnumerable<string>? pathFilters,
            string userAgent,
            string userAccepts) 
        {
            CrawlPageRequest = new CrawlPageRequestDto
            {
                Url = url,
                GraphId = graphId,
                CorrelationId = correlationId,
                Attempt = 1,
                Depth = 0,
                MaxDepth = maxDepth,
                FollowExternalLinks = followExternalLinks,
                RemoveQueryStrings = removeQueryStrings,
                PathFilters = pathFilters,
                UserAgent = userAgent,
                UserAccepts = userAccepts,
                RequestedAt = DateTimeOffset.UtcNow
            };
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public CrawlPageEvent(
            Uri url,
            int graphId,
            bool followExternalLinks,
            bool removeQueryStrings,
            int maxDepth,
            IEnumerable<string>? pathFilters,
            string? userAgent,
            string? userAccepts)
            : this(
                url,
                graphId,
                correlationId: Guid.NewGuid(),
                attempt: 1,
                depth: 0,
                maxDepth,
                followExternalLinks,
                removeQueryStrings,
                pathFilters,
                userAgent,
                userAccepts) { }


        public CrawlPageEvent(
            Uri url,
            int attempt,
            int depth,
            CrawlPageRequestDto crawlPageRequest)
            : this(
                url,
                crawlPageRequest.GraphId,
                crawlPageRequest.CorrelationId,
                attempt,
                depth,
                crawlPageRequest.MaxDepth,
                crawlPageRequest.FollowExternalLinks,
                crawlPageRequest.RemoveQueryStrings,
                crawlPageRequest.PathFilters,
                crawlPageRequest.UserAgent,
                crawlPageRequest.UserAccepts) { } 

    }
}

using System;

namespace Events.Core.EventTypes
{
    public record CrawlPageEvent
    {
        public Uri Url { get; init; }
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public int MapId { get; init; }
        public Guid CorrelationId { get; init; }
        public bool FollowExternalLinks { get; init; }
        public int MaxDepth { get; init; }
        public bool RemoveQueryStrings { get; init; }
        public IEnumerable<string>? PathFilters { get; init; }
        public string UserAgent { get; init; }
        public string UserAccepts { get; init; }
        public DateTimeOffset CreatedAt { get; init; }


        private CrawlPageEvent() { }

        public CrawlPageEvent(
            Uri url, 
            int mapId, 
            Guid correlationId, 
            bool followExternalLinks,
            bool removeQueryStrings,
            int maxDepth,
            IEnumerable<string>? pathFilters,
            string? userAgent,
            string? userAccepts) { 

            Url = url;
            MapId = mapId;
            CorrelationId = correlationId;
            FollowExternalLinks = followExternalLinks;
            RemoveQueryStrings = removeQueryStrings;
            MaxDepth = maxDepth;
            PathFilters = pathFilters;
            UserAgent = userAgent;
            UserAccepts = userAccepts;
        }

        public CrawlPageEvent(CrawlPageEvent evt, Uri url, int attempt, int depth) 
        {
            Url = url;
            Attempt = attempt;
            Depth = depth;
            MapId = evt.MapId;
            CorrelationId = evt.CorrelationId;
            FollowExternalLinks = evt.FollowExternalLinks;
            RemoveQueryStrings = evt.RemoveQueryStrings;
            MaxDepth = evt.MaxDepth;
            PathFilters = evt.PathFilters;
            UserAgent = evt.UserAgent;
            UserAccepts = evt.UserAccepts;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}

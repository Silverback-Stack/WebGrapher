using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.EventTypes
{
    public record CrawlPageEvent
    {
        public Uri Url { get; init; }
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public Guid ContainerId { get; init; }
        public Guid CorrelationId { get; init; }
        public bool FollowExternalLinks { get; init; }
        public int MaxDepth { get; init; }
        public bool RemoveQueryStrings { get; init; }
        public IEnumerable<string>? PathFilters { get; init; }
        public string? UserAgent { get; init; }
        public string? UserAccepts { get; init; }
        public DateTimeOffset CreatedAt { get; init; }


        private CrawlPageEvent() { }

        public CrawlPageEvent(
            Uri url, 
            Guid containerId, 
            Guid correlationId, 
            bool followExternalLinks,
            bool removeQueryStrings,
            int maxDepth,
            IEnumerable<string>? pathFilters,
            string? userAgent,
            string? userAccepts) { 

            Url = url;
            ContainerId = containerId;
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
            ContainerId = evt.ContainerId;
            CorrelationId = evt.CorrelationId;
            FollowExternalLinks = evt.FollowExternalLinks;
            MaxDepth = evt.MaxDepth;
            PathFilters = evt.PathFilters;
            UserAgent = evt.UserAgent;
            UserAccepts = evt.UserAccepts;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}

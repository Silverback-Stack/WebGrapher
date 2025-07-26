using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.EventTypes
{
    public record CrawlPageEvent
    {
        public Uri Url { get; set; }
        public int Attempt { get; set; }
        public Guid ContainerId { get; set; }
        public Guid CorrelationId { get; set; }
        public bool FollowExternalLinks { get; set; }
        public bool RemoveQueryStrings { get; set; }
        public int MaxDepth { get; set; }
        public int Depth { get; set; }
        public IEnumerable<string>? PathFilters { get; set; }
        public string? UserAgent { get; set; }
        public string? UserAccepts { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        private CrawlPageEvent() { }

        public CrawlPageEvent(Uri url, Guid containerId, Guid correlationId) { 
            Url = url;
            ContainerId = containerId;
            CorrelationId = correlationId;
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

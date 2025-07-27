using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.EventTypes
{
    public record GraphPageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri Url { get; init; }
        public string Title { get; init; }
        public string Keywords { get; init; }

        public IEnumerable<string> Links { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? SourceLastModified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}

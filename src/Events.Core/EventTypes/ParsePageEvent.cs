using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.EventTypes
{
    public record ParsePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri Url { get; init; }
        public required string Content { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}

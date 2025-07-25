using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.EventTypes
{
    public record ScrapePageResultEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; set; }
        public HttpStatusCode StatusCode {  get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public DateTimeOffset? RetryAfter { get; set; }
        public Uri? RedirectLocation { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Types
{
    public record GraphPageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; set; }
        public string Title { get; set; }
        public string Keywords { get; set; }

        public IEnumerable<string> Links;
        public HttpStatusCode StatusCode { get; set; }
        public DateTimeOffset SourceLastModified { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

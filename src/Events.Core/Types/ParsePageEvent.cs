using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Types
{
    public record ParsePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; set; }
        public string HtmlContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

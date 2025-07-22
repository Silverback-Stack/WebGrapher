using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScraperService
{
    public record ScrapeResponse
    {
        public required string HtmlContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }
}

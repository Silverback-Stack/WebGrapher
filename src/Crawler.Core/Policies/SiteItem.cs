using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core.Policies
{
    public record SiteItem
    {
        public required Uri Url { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string RobotsTxtContent { get; set; } = string.Empty;
        public DateTimeOffset FetchedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RetryAfter { get; set; }
    }
}

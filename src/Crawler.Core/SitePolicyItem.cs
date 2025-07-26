using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core
{
    public record SitePolicyItem
    {
        public required string UrlAuthority { get; set; }
        public string? RobotsTxtContent { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RetryAfter { get; set; }
    }
}

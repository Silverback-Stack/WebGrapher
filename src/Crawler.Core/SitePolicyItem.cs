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
        public required string UrlAuthority { get; init; }
        public string? RobotsTxtContent { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset ModifiedAt { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
    }
}

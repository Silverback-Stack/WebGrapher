using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitePolicy.Core
{
    public record SiteRobotsPolicyItem
    {
        public required string UrlAuthority { get; init; }
        public string? RobotsTxt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ModifiedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }
}

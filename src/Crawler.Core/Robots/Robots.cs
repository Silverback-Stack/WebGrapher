using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core.Robots
{
    public class RobotsItem
    {
        public required Uri Url { get; set; }
        public string RobotsTxtContent { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

}

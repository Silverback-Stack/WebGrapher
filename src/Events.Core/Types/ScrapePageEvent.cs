using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Types
{
    public record ScrapePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

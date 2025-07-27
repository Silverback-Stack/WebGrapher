using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserService
{
    public record PageItem
    {
        public string? Title {  get; init; }
        public string? Content { get; init; }

        public DateTimeOffset LastModified { get; init; }

        public IEnumerable<string> Links { get; init; } = new List<string>();
    }
}

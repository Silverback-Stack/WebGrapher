using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Core.Dtos
{
    public record PageNodeDto
    {
        public string Url { get; init; }
        public string Title { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public List<PageNodeDto> Links { get; init; } = new();
    }
}

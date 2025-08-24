using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Dtos
{
    public class ListGraphDto
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
        public string Url { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Requests.Core
{
    public record CacheInfo
    {
        public bool IsFromCache { get; init; }
        public string? Key { get; init; }
        public string? Container { get; init; }
        public string? PartitionKey { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logging.Core
{
    public record LogItem
    {
        public string? Service { get; set; }
        public string Message { get; set; }
        public string? CorrelationId { get; set; }
        public object? Context { get; set; }
        public AppLoggerLevel Level { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}

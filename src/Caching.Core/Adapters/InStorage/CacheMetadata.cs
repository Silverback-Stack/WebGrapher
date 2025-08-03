using System.Net.Mime;
using System.Text;

namespace Caching.Core.Adapters.InStorage
{
    public partial class InStorageCacheAdapter
    {
        public class CacheMetadata
        {
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}

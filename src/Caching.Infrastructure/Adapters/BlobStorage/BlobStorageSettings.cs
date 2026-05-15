using System;

namespace Caching.Infrastructure.Adapters.BlobStorage
{
    public class BlobStorageSettings
    {
        public int AbsoluteExpiryHours { get; set; } = 24;
        public string ConnectionString { get; set; } = string.Empty;
    }
}

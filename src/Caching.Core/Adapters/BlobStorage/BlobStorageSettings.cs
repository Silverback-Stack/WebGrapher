using System;

namespace Caching.Core.Adapters.BlobStorage
{
    public class BlobStorageSettings
    {
        public string ContainerName { get; set; } = "blob.cache";
        public int AbsoluteExpiryHours { get; set; } = 24;
        public string ConnectionString { get; set; } = string.Empty;
    }
}

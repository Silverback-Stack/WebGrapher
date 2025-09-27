using System;

namespace Caching.Core.Adapters.BlobStorage
{
    public class BlobStorageSettings
    {
        public string ContainerName { get; set; } = "blob.cache";
        public int AbsoluteExpirationHours { get; set; } = 0;
        public string ConnectionString { get; set; } = string.Empty;
    }
}

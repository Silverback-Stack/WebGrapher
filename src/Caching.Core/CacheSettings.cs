using Caching.Core.Adapters.BlobStorage;
using Caching.Core.Adapters.FileStorage;
using Caching.Core.Adapters.Redis;

namespace Caching.Core
{
    public class CacheSettings
    {
        public CacheType CacheType { get; set; } = CacheType.Memory;

        public FileStorageSettings FileStorage { get; set; } = new FileStorageSettings();

        public RedisSettings Redis { get; set; } = new RedisSettings();

        public BlobStorageSettings BlobStorage { get; set; } = new BlobStorageSettings();
    }
}

using Caching.Infrastructure.Adapters.BlobStorage;
using Caching.Infrastructure.Adapters.FileStorage;
using Caching.Infrastructure.Adapters.Redis;
using System;

namespace Caching.Factories
{
    public class CacheConfig
    {
        public CacheProvider Provider { get; set; } = CacheProvider.Memory;

        public string Container { get; set; } = "default.cache";

        public FileStorageSettings FileStorage { get; set; } = new FileStorageSettings();

        public RedisCacheSettings Redis { get; set; } = new RedisCacheSettings();

        public BlobStorageSettings BlobStorage { get; set; } = new BlobStorageSettings();
    }
}

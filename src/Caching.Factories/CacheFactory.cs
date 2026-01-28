using System;
using Microsoft.Extensions.Logging;
using Caching.Core;
using Caching.Infrastructure.Adapters.Memory;
using Caching.Infrastructure.Adapters.FileStorage;
using Caching.Infrastructure.Adapters.Redis;
using Caching.Infrastructure.Adapters.BlobStorage;

namespace Caching.Factories
{
    public static class CacheFactory
    {
        public static ICache Create(string serviceName, ILogger logger, CacheConfig cacheSettings)
        {
            switch (cacheSettings.Provider)
            {
                case CacheProvider.Memory:
                    return new MemoryCacheAdapter(serviceName, logger);

                case CacheProvider.FileStorage:
                    return new FileStorageCacheAdapter(serviceName, logger, cacheSettings.FileStorage);

                case CacheProvider.Redis:
                    return new RedisCacheAdapter(serviceName, logger, cacheSettings.Redis);

                case CacheProvider.BlobStorage:
                    return new BlobStorageCacheAdapter(serviceName, logger, cacheSettings.BlobStorage);

                default:
                    throw new NotSupportedException($"Caching option '{cacheSettings.Provider}' is not supported.");
            }
        }

    }
}

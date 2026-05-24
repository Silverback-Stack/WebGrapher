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
        public static ICache Create(ILogger logger, CacheConfig cacheConfig)
        {
            if (cacheConfig is null)
                throw new ArgumentNullException(nameof(cacheConfig));

            switch (cacheConfig.Provider)
            {
                case CacheProvider.Memory:
                    return new MemoryCacheAdapter(logger, cacheConfig.Container);

                case CacheProvider.FileStorage:
                    return new FileStorageCacheAdapter(logger, cacheConfig.Container, cacheConfig.FileStorage);

                case CacheProvider.Redis:
                    return new RedisCacheAdapter(logger, cacheConfig.Container, cacheConfig.Redis);

                case CacheProvider.BlobStorage:
                    return new BlobStorageCacheAdapter(logger,cacheConfig.Container, cacheConfig.BlobStorage);

                default:
                    throw new NotSupportedException($"Caching option '{cacheConfig.Provider}' is not supported.");
            }
        }

    }
}

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
        public static ICache Create(ILogger logger, CacheConfig cacheSettings)
        {
            if (cacheSettings is null)
                throw new ArgumentNullException(nameof(cacheSettings));

            switch (cacheSettings.Provider)
            {
                case CacheProvider.Memory:
                    return new MemoryCacheAdapter(logger, cacheSettings.Container);

                case CacheProvider.FileStorage:
                    return new FileStorageCacheAdapter(logger, cacheSettings.Container, cacheSettings.FileStorage);

                case CacheProvider.Redis:
                    return new RedisCacheAdapter(logger, cacheSettings.Container, cacheSettings.Redis);

                case CacheProvider.BlobStorage:
                    return new BlobStorageCacheAdapter(logger,cacheSettings.Container, cacheSettings.BlobStorage);

                default:
                    throw new NotSupportedException($"Caching option '{cacheSettings.Provider}' is not supported.");
            }
        }

    }
}

using System;
using Caching.Core.Adapters.Memory;
using Caching.Core.Adapters.FileStorage;
using Caching.Core.Adapters.Redis;
using Microsoft.Extensions.Logging;
using Caching.Core.Adapters.BlobStorage;

namespace Caching.Core
{
    public static class CacheFactory
    {
        public static ICache CreateCache(string serviceName, ILogger logger, CacheSettings cacheSettings)
        {
            switch (cacheSettings.Provider)
            {
                case CacheProvider.Memory:
                    return new MemoryCacheAdapter(serviceName, logger);

                case CacheProvider.FileStorage:
                    return new FileStorageCacheAdapter(serviceName, logger, cacheSettings);

                case CacheProvider.Redis:
                    return new RedisCacheAdapter(serviceName, logger, cacheSettings);

                case CacheProvider.BlobStorage:
                    return new BlobStorageCacheAdapter(serviceName, logger, cacheSettings);

                default:
                    throw new NotSupportedException($"Caching option '{cacheSettings.Provider}' is not supported.");
            }
        }
    }
}

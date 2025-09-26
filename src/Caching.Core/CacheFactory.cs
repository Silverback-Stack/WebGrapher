using System;
using Caching.Core.Adapters.InMemory;
using Caching.Core.Adapters.InStorage;
using Caching.Core.Adapters.Redis;
using Microsoft.Extensions.Logging;

namespace Caching.Core
{
    public static class CacheFactory
    {
        public static ICache CreateCache(string serviceName, ILogger logger, CacheSettings cacheSettings)
        {
            switch (cacheSettings.CacheType)
            {
                case CacheType.InMemory:
                    return new InMemoryCacheAdapter(serviceName, logger);

                case CacheType.InStorage:
                    return new InStorageCacheAdapter(serviceName, logger, cacheSettings);

                case CacheType.Redis:
                    return new RedisCacheAdapter(serviceName, logger, cacheSettings);

                case CacheType.AzureBlobStorage:
                    throw new NotSupportedException($"Caching option '{cacheSettings.CacheType}' is not supported.");

                default:
                    throw new NotSupportedException($"Caching option '{cacheSettings.CacheType}' is not supported.");
            }
        }
    }
}

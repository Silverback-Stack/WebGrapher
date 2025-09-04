using System;
using Caching.Core.Adapters.InMemory;
using Caching.Core.Adapters.InStorage;
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
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException($"Caching option '{cacheSettings.CacheType}' is not supported.");
            }
        }
    }
}

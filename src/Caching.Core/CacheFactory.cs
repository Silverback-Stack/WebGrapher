using System;
using Caching.Core.Adapters.InMemory;
using Caching.Core.Adapters.InStorage;
using Microsoft.Extensions.Logging;

namespace Caching.Core
{
    public static class CacheFactory
    {
        public static ICache CreateCache(CacheSettings settings, string serviceName, ILogger logger)
        {
            switch (settings.CacheType)
            {
                case CacheType.InMemory:
                    return new InMemoryCacheAdapter(serviceName, logger);

                case CacheType.InStorage:
                    return new InStorageCacheAdapter(settings, serviceName, logger);

                case CacheType.Redis:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException($"Caching option '{settings.CacheType}' is not supported.");
            }
        }
    }
}

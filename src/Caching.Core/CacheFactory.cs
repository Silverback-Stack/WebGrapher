using System;
using Caching.Core.Adapters.InMemory;
using Caching.Core.Adapters.InStorage;
using Microsoft.Extensions.Logging;

namespace Caching.Core
{
    public static class CacheFactory
    {
        public static ICache CreateCache(string serviceName, CacheOptions options, ILogger logger)
        {
            switch (options)
            {
                case CacheOptions.InMemory:
                    return new InMemoryCacheAdapter(serviceName, logger);

                case CacheOptions.InStorage:
                    return new InStorageCacheAdapter(serviceName, logger);

                case CacheOptions.Redis:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException($"Caching option '{options}' is not supported.");
            }
        }
    }
}

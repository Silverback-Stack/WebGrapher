using System;
using Caching.Core.Adapters.Memory;
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
                    return new MemoryCacheAdapter(serviceName, logger);

                case CacheOptions.Redis:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException($"Caching option '{options}' is not supported.");
            }
        }
    }
}

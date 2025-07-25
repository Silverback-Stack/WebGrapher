using System;
using Logging.Core;

namespace Caching.Core
{
    public static class CacheFactory
    {
        public static ICache CreateCache(CacheOptions options, ILogger logger)
        {
            switch (options)
            {
                case CacheOptions.InMemory:
                    return new MemoryCacheAdapter(logger);

                case CacheOptions.Redis:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException($"Caching option '{options}' is not supported.");
            }
        }
    }
}

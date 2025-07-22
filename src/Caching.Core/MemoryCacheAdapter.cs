using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Core
{
    public class MemoryCacheAdapter : ICache
    {
        private readonly IAppLogger _appLogger;
        private readonly IMemoryCache _cache;

        public MemoryCacheAdapter(IAppLogger appLogger)
        {
            _appLogger = appLogger;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public T? Get<T>(string key)
        {
            _appLogger.LogInformation("Getting {key} from the cache.");
            return _cache.TryGetValue(key, out var value) ? (T?)value : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _appLogger.LogInformation("Setting {key} in the cache.");
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            _cache.Set(key, value, options);
        }

        public void Remove(string key)
        {
            _appLogger.LogInformation("Removing {key} from the cache.");
            _cache.Remove(key);
        }

        public bool Exists(string key)
        {
            _appLogger.LogInformation("Checking {key} is in the cache.");
            return _cache.TryGetValue(key, out _);
        }
    }
}

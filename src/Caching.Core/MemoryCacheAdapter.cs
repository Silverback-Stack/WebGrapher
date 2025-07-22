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
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public MemoryCacheAdapter(ILogger logger)
        {
            _logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public T? Get<T>(string key)
        {
            _logger.LogInformation("Getting {key} from the cache.");
            return _cache.TryGetValue(key, out var value) ? (T?)value : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _logger.LogInformation("Setting {key} in the cache.");
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            _cache.Set(key, value, options);
        }

        public void Remove(string key)
        {
            _logger.LogInformation("Removing {key} from the cache.");
            _cache.Remove(key);
        }

        public bool Exists(string key)
        {
            _logger.LogInformation("Checking {key} is in the cache.");
            return _cache.TryGetValue(key, out _);
        }

        public void Dispose()
        {
            if (_cache is IDisposable disposable)
            {
                _logger.LogInformation($"Disposing: {typeof(MemoryCacheAdapter).Name}.");
                disposable.Dispose();
            }
        }
    }
}

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
            var item = _cache.TryGetValue(key, out var value) ? (T?)value : default;

            if (item == null)
                _logger.LogInformation($"Cache Miss: {key} was not found in the cache.");
            else
                _logger.LogInformation($"Cache Hit: {key} was found in the cache.");

            return item;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            var item = _cache.Set(key, value, options);

            if (item == null)
                _logger.LogInformation($"Cache Set Failed: {key} was not stored to the cache.");
            else
                _logger.LogInformation($"Cache Set: {key} was stored to the cache.");
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public bool Exists(string key)
        {
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

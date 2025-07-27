using System;
using Logging.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Core
{
    public class MemoryCacheAdapter : ICache
    {
        private readonly string _serviceName;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public MemoryCacheAdapter(string serviceName, ILogger logger)
        {
            _serviceName = serviceName; 
            _logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        private string GetScopedKey(string key) => $"{_serviceName}_{key}";

        public async Task<T?> GetAsync<T>(string key)
        {
            key = GetScopedKey(key);
            var item = _cache.TryGetValue(key, out var value) ? (T?)value : default;

            //if (item == null)
            //    _logger.LogDebug($"Cache Miss: {typeof(T).Name} with key {key} was not found in the cache.");
            //else
            //    _logger.LogDebug($"Cache Hit: {typeof(T).Name} with key {key} was found in the cache.", context: item);

            return item;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key);
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            var item = _cache.Set(key, value, options);

            //if (item == null)
            //    _logger.LogDebug($"Cache Set Failed: {typeof(T).Name} with key {key} was not stored to the cache.", context: item);
            //else
            //    _logger.LogDebug($"Cache Set: {typeof(T).Name} with key {key} was stored to the cache.", context: item);
        }

        public async Task RemoveAsync(string key)
        {
            key = GetScopedKey(key);
            _cache.Remove(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            key = GetScopedKey(key);
            return _cache.TryGetValue(key, out _);
        }

        public void Dispose()
        {
            if (_cache is IDisposable disposable)
            {
                _logger.LogDebug($"Disposing: {typeof(MemoryCacheAdapter).Name}.");
                disposable.Dispose();
            }
        }
    }
}

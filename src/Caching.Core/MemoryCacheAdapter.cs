using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
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
            return _cache.TryGetValue(key, out var value) ? (T?)value : default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key);
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            _cache.Set(key, value, options);
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

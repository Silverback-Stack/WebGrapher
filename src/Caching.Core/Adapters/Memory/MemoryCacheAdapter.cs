using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Core.Adapters.Memory
{
    public class MemoryCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// In-memory cache adapter for local development.
        /// </summary>
        public MemoryCacheAdapter(string serviceName, ILogger logger)
        {
            Container = serviceName;
            _logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public string Container {  get; private set; }

        private string GetScopedKey(string key) => $"{Container}_{key}";

        public Task<T?> GetAsync<T>(string key)
        {
            key = GetScopedKey(key);

            if (_cache.TryGetValue(key, out var value))
            {
                _logger.LogDebug($"Cache hit for {key}");

                return Task.FromResult((T?)value);
            }

            _logger.LogDebug($"Cache miss for {key}");

            return Task.FromResult(default(T?));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key);
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
                _logger.LogDebug($"Setting cache for {key} with TTL: {expiration.Value}");
            }
            else
            {
                _logger.LogDebug($"Setting cache for {key} with no expiration");
            }

            _cache.Set(key, value, options);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            key = GetScopedKey(key);
            _logger.LogDebug($"Removing cache entry for {key}");

            _cache.Remove(key);

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            key = GetScopedKey(key);
            var exists = _cache.TryGetValue(key, out _);
            _logger.LogDebug($"Checking existence for {key}: {exists}");

            return Task.FromResult(exists);
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

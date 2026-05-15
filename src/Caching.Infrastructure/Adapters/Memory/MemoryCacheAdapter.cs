using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Caching.Core;

namespace Caching.Infrastructure.Adapters.Memory
{
    public class MemoryCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public string Container { get; }

        /// <summary>
        /// In-memory cache adapter for local development.
        /// </summary>
        public MemoryCacheAdapter(ILogger logger, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            _logger = logger;

            Container = container.Trim();

            _cache = new MemoryCache(new MemoryCacheOptions());
        }


        private string GetScopedKey(string key, string container)
        {
            return $"{container}:{key}";
        }


        private Task<T?> GetInternalAsync<T>(string key, string container)
        {
            key = GetScopedKey(key, container);

            if (_cache.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Cache hit for {Key}", key);

                return Task.FromResult((T?)value);
            }

            _logger.LogDebug("Cache miss for {Key}", key);

            return Task.FromResult(default(T?));
        }


        public Task<T?> GetAsync<T>(string key)
        {
            return GetInternalAsync<T>(key, Container);
        }


        public Task<T?> GetFromContainerAsync<T>(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return GetInternalAsync<T>(key, container);
        }


        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key, Container);
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
                _logger.LogDebug("Setting cache for {Key} with TTL: {Value}", key, expiration.Value);
            }
            else
            {
                _logger.LogDebug("Setting cache for {Key} with no expiration", key);
            }

            _cache.Set(key, value, options);

            return Task.CompletedTask;
        }


        public Task RemoveAsync(string key)
        {
            key = GetScopedKey(key, Container);
            _logger.LogDebug("Removing cache entry for {Key}", key);

            _cache.Remove(key);

            return Task.CompletedTask;
        }


        private Task<bool> ExistsInternalAsync(string key, string container)
        {
            key = GetScopedKey(key, container);

            var exists = _cache.TryGetValue(key, out _);

            _logger.LogDebug(
                "Checking existence for {Key}: {Exists}",
                key,
                exists);

            return Task.FromResult(exists);
        }


        public Task<bool> ExistsAsync(string key)
        {
            return ExistsInternalAsync(key, Container);
        }
        

        public Task<bool> ExistsInContainerAsync(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return ExistsInternalAsync(key, container);
        }


        public void Dispose()
        {
            if (_cache is IDisposable disposable)
            {
                _logger.LogDebug("Disposing: {Name}", typeof(MemoryCacheAdapter).Name);
                disposable.Dispose();
            }
        }

    }
}

using System;
using System.Text.Json;
using Caching.Core;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Caching.Infrastructure.Adapters.Redis
{
    public class RedisCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly ConnectionMultiplexer _connection;
        private readonly IDatabase _database;

        public string Container { get; }

        public RedisCacheAdapter(
            ILogger logger,
            string container, 
            RedisCacheSettings cacheSettings)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            if (string.IsNullOrWhiteSpace(cacheSettings.ConnectionString))
                throw new ArgumentException(
                    "Redis connection string is required.",
                    nameof(cacheSettings.ConnectionString));

            _logger = logger;

            Container = container.Trim();

            _connection = ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
            _database = _connection.GetDatabase();
        }


        private string GetScopedKey(string key, string container)
        {
            return $"{container}:{key}";
        }


        public async Task<T?> GetInternalAsync<T>(string key, string container)
        {
            key = GetScopedKey(key, container);

            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for {Key}", key);
                return default;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(value!);

                _logger.LogDebug("Cache hit for {Key}", key);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to deserialize cache entry for {Key}", 
                    key);

                return default;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            return await GetInternalAsync<T>(key, Container);
        }

        public async Task<T?> GetFromContainerAsync<T>(string key, string container)
        {
            return await GetInternalAsync<T>(key, container);
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key, Container);

            var json = JsonSerializer.Serialize(value);

            if (expiration.HasValue)
                _logger.LogDebug("Setting cache for {Key} with TTL: {Value}", key, expiration.Value);
            else
                _logger.LogDebug("Setting cache for {Key} with no expiration", key);

            // Convert nullable TimeSpan to Redis Expiration
            Expiration expiry = expiration.HasValue 
                ? new Expiration(expiration.Value) 
                : Expiration.Default;

            await _database.StringSetAsync(key, json, expiry);
        }


        public async Task RemoveAsync(string key)
        {
            key = GetScopedKey(key, Container);
            _logger.LogDebug("Removing cache entry for {Key}", key);

            await _database.KeyDeleteAsync(key);
        }


        public async Task<bool> ExistsInternalAsync(string key, string container)
        {
            key = GetScopedKey(key, container);

            var exists = await _database.KeyExistsAsync(key);

            _logger.LogDebug(
                "Checking existence for {Key}: {Exists}", 
                key, 
                exists);

            return exists;
        }


        public async Task<bool> ExistsAsync(string key)
        {
            return await ExistsInternalAsync(key, Container);
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
            _logger.LogDebug("Disposing: {Name}", typeof(RedisCacheAdapter).Name);
            _connection.Dispose();
        }

    }
}

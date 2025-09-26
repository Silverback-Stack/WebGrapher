using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Caching.Core.Adapters.Redis
{
    public class RedisCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly ConnectionMultiplexer _connection;
        private readonly IDatabase _database;

        public RedisCacheAdapter(string serviceName, ILogger logger, CacheSettings cacheSettings)
        {
            Container = serviceName;
            _logger = logger;

            _connection = ConnectionMultiplexer.Connect(cacheSettings.Redis.ConnectionString);
            _database = _connection.GetDatabase();
        }

        public string Container { get; private set; }

        private string GetScopedKey(string key) => $"{Container}_{key}";

        public async Task<T?> GetAsync<T>(string key)
        {
            key = GetScopedKey(key);

            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug($"Cache miss for {key}");
                return default;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(value!);
                _logger.LogDebug($"Cache hit for {key}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deserialize cache entry for {key}");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            key = GetScopedKey(key);

            var json = JsonSerializer.Serialize(value);
            var expiry = expiration ?? (TimeSpan?)null;

            if (expiry.HasValue)
                _logger.LogDebug($"Setting cache for {key} with TTL: {expiry.Value}");
            else
                _logger.LogDebug($"Setting cache for {key} with no expiration");

            await _database.StringSetAsync(key, json, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            key = GetScopedKey(key);
            _logger.LogDebug($"Removing cache entry for {key}");

            await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            key = GetScopedKey(key);
            var exists = await _database.KeyExistsAsync(key);
            _logger.LogDebug($"Checking existence for {key}: {exists}");
            return exists;
        }

        public void Dispose()
        {
            _logger.LogDebug($"Disposing: {typeof(RedisCacheAdapter).Name}.");
            _connection.Dispose();
        }
    }
}

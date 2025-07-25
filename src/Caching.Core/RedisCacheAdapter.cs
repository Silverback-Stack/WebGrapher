using System;
using System.Text.Json;
using Logging.Core;
using StackExchange.Redis;

namespace Caching.Core
{
    public class RedisCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly IDatabase _db;

        public RedisCacheAdapter(ILogger logger, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            _logger.LogInformation("Getting {key} from the cache.");
            var value = await _db.StringGetAsync(key);
            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value)
                : default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            _logger.LogInformation("Setting {key} in the cache.");
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration);
        }

        public async Task RemoveAsync(string key)
        {
            _logger.LogInformation("Removing {key} from the cache.");
            await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            _logger.LogInformation("Checking {key} is in the cache.");
            return await _db.KeyExistsAsync(key);
        }

        public void Dispose()
        {
            if (_db is IDisposable disposable)
            {
                _logger.LogInformation($"Disposing: {typeof(RedisCacheAdapter).Name}.");
                disposable.Dispose();
            }
        }
    }
}

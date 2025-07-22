using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        public T? Get<T>(string key)
        {
            _logger.LogInformation("Getting {key} from the cache.");
            var value = _db.StringGet(key);
            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value)
                : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _logger.LogInformation("Setting {key} in the cache.");
            var json = JsonSerializer.Serialize(value);
            _db.StringSet(key, json, expiration);
        }

        public void Remove(string key)
        {
            _logger.LogInformation("Removing {key} from the cache.");
            _db.KeyDelete(key);
        }

        public bool Exists(string key)
        {
            _logger.LogInformation("Checking {key} is in the cache.");
            return _db.KeyExists(key);
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

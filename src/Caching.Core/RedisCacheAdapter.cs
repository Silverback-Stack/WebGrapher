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
        private readonly IAppLogger _appLogger;
        private readonly IDatabase _db;

        public RedisCacheAdapter(IAppLogger appLogger, IConnectionMultiplexer redis)
        {
            _appLogger = appLogger;
            _db = redis.GetDatabase();
        }

        public T? Get<T>(string key)
        {
            _appLogger.LogInformation("Getting {key} from the cache.");
            var value = _db.StringGet(key);
            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value)
                : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _appLogger.LogInformation("Setting {key} in the cache.");
            var json = JsonSerializer.Serialize(value);
            _db.StringSet(key, json, expiration);
        }

        public void Remove(string key)
        {
            _appLogger.LogInformation("Removing {key} from the cache.");
            _db.KeyDelete(key);
        }

        public bool Exists(string key)
        {
            _appLogger.LogInformation("Checking {key} is in the cache.");
            return _db.KeyExists(key);
        }
    }
}

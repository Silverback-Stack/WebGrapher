using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Caching.Core
{
    public class RedisCacheAdapter : ICache
    {
        private readonly IDatabase _db;

        public RedisCacheAdapter(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public T? Get<T>(string key)
        {
            var value = _db.StringGet(key);
            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value)
                : default;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(value);
            _db.StringSet(key, json, expiration);
        }

        public void Remove(string key)
        {
            _db.KeyDelete(key);
        }

        public bool Exists(string key)
        {
            return _db.KeyExists(key);
        }
    }
}

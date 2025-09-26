using Caching.Core.Adapters.InStorage;
using Caching.Core.Adapters.Redis;

namespace Caching.Core
{
    public class CacheSettings
    {
        public CacheType CacheType { get; set; } = CacheType.InMemory;

        public InStorageSettings InStorage { get; set; } = new InStorageSettings();

        public RedisSettings Redis { get; set; } = new RedisSettings();
    }
}

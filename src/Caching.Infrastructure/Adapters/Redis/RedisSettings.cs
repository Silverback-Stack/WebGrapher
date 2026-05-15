using System;

namespace Caching.Infrastructure.Adapters.Redis
{
    public class RedisCacheSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
}

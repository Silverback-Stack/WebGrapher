using Caching.Core.Adapters.InStorage;
using Caching.Core.Helpers;

namespace Caching.Core
{
    public class CacheSettings
    {
        public CacheType CacheType { get; set; } = CacheType.InMemory;

        public InStorageSettings InStorage { get; set; } = new InStorageSettings();
    }
}

namespace Caching.Core
{
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Retrieve a cached item by key.
        /// </summary>
        T? Get<T>(string key);

        /// <summary>
        /// Add or update a cached item with expiration.
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Remove an item from the cache.
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Check if a key exists in the cache.
        /// </summary>
        bool Exists(string key);
    }
}

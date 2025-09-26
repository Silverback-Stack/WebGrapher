
namespace Caching.Core
{
    public interface ICache : IDisposable
    {
        string Container { get; }

        /// <summary>
        /// Retrieve a cached item by key.
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Add or update a cached item with expiration.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Remove an item from the cache.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Check if a key exists in the cache.
        /// </summary>
        Task<bool> ExistsAsync(string key);
    }
}


namespace Caching.Core
{
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Logical cache boundary used to group and share cached data across services.
        /// </summary>
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


        /// <summary>
        /// Retrieve a cached item from a specific logical container.
        /// Used when reading cached data produced by another service.
        /// </summary>
        Task<T?> GetFromContainerAsync<T>(string key, string container);

        /// <summary>
        /// Check if a key exists in a specific logical container.
        /// Used when checking for cached data produced by another service.
        /// </summary>
        Task<bool> ExistsInContainerAsync(string key, string container);

    }
}

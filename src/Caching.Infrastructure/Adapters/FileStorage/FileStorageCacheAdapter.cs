using System;
using System.Text.Json;
using Caching.Core;
using Microsoft.Extensions.Logging;

namespace Caching.Infrastructure.Adapters.FileStorage
{
    public class FileStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly FileStorageSettings _fileStorageSettings;
        private readonly string _containerPath;

        public string Container { get; }

        /// <summary>
        /// In-storage cache adapter for local development.
        /// </summary>
        public FileStorageCacheAdapter(
            ILogger logger,
            string container,
            FileStorageSettings fileStorageSettings)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            _logger = logger;
            _fileStorageSettings = fileStorageSettings;
            Container = container.Trim();

            _containerPath = CreateContainer(Container);

            //fire on background thread
            _ = ClearCacheAsync(fileStorageSettings.AbsoluteExpiryHours);
        }


        /// <summary>
        /// Removes expired cache files using synchronous file system operations.
        /// </summary>
        private Task ClearCacheAsync(int absoluteExpiryHours)
        {
            // Calculate the expiration cutoff time.
            // Files older than this timestamp will be removed.
            var cutoff = DateTime.UtcNow.AddHours(-absoluteExpiryHours);

            // Enumerate all cached files within the container directory.
            foreach (var file in Directory.GetFiles(_containerPath))

                // Delete files whose last modified timestamp is older than the cutoff.
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                    File.Delete(file);

            return Task.CompletedTask;
        }


        /// <summary>
        /// Creates or opens the physical folder for a cache container.
        /// </summary>
        private string CreateContainer(string container)
        {
            // File system folder names may contain invalid characters
            // depending on the operating system and file system.
            // Normalize the logical container name into a filesystem-safe folder name.

            var invalidChars = Path.GetInvalidFileNameChars();

            // Replace invalid filesystem characters with hyphens
            var folderName = string.Concat(
                container
                    .ToLowerInvariant()
                    .Select(c => invalidChars.Contains(c) ? '-' : c));

            // Map the logical cache container to a physical folder path
            var containerPath = Path.Combine(AppContext.BaseDirectory, folderName);

            try
            {
                Directory.CreateDirectory(containerPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Unable to create cache container {Path}",
                    containerPath);
                throw;
            }

            return containerPath;
        }


        /// <summary>
        /// Returns the physical file path of a cached object.
        /// </summary>
        private string GetFilePath(string key, string container)
        {
            var containerPath = CreateContainer(container);

            return Path.Combine(containerPath, key);
        }


        /// <summary>
        /// Checks whether a cached file has expired.
        /// </summary>
        private bool IsExpired(string path, int absoluteExpiryHours)
        {
            var cutoff = DateTime.UtcNow.AddHours(-absoluteExpiryHours);

            return File.GetLastWriteTimeUtc(path) < cutoff;
        }


        /// <summary>
        /// Retrieves an object from the specified cache container.
        /// </summary>
        private async Task<T?> GetInternalAsync<T>(string key, string container)
        {
            try
            {
                var path = GetFilePath(key, container);
                if (!File.Exists(path)) return default;

                var expired = IsExpired(path, _fileStorageSettings.AbsoluteExpiryHours);
                if (expired) return default;

                // Return raw binary content for byte[] requests.
                if (typeof(T) == typeof(byte[]))
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    return (T)(object)bytes;
                }

                // Return an open read stream for Stream requests.
                if (typeof(T) == typeof(Stream))
                {
                    var stream = File.OpenRead(path);
                    return (T)(object)stream;
                }

                // Read JSON content and deserialize into the requested type.
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to read file {Key} from container {Container}",
                    key,
                    container);

                return default;
            }
        }


        /// <summary>
        /// Checks whether an object exists in the specified cache container.
        /// </summary>
        private Task<bool> ExistsInternalAsync(string key, string container)
        {
            var path = GetFilePath(key, container);
            if (!File.Exists(path)) return Task.FromResult(false);

            var expired = IsExpired(path, _fileStorageSettings.AbsoluteExpiryHours);

            return Task.FromResult(!expired);
        }


        public void Dispose()
        {
            //nothing to do
        }


        /// <summary>
        /// Checks whether an object exists in the default cache container.
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            return await ExistsInternalAsync(key, Container);
        }


        /// <summary>
        /// Checks whether an object exists in the specified cache container.
        /// </summary>
        public async Task<bool> ExistsInContainerAsync(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return await ExistsInternalAsync(key, container);
        }


        /// <summary>
        /// Retrieves an object from the default cache container.
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            return await GetInternalAsync<T>(key, Container);
        }


        /// <summary>
        /// Retrieves an object from the specified cache container.
        /// </summary>
        public async Task<T?> GetFromContainerAsync<T>(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return await GetInternalAsync<T>(key, container);
        }


        /// <summary>
        /// Removes an object from the default cache container.
        /// </summary>
        public Task RemoveAsync(string key)
        {
            var path = GetFilePath(key, Container);

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to delete cache file {Path}", path);
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Adds or updates an object in the default cache container.
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            // File storage expiration is handled by background cleanup
            // using LastModified timestamps rather than per-object TTL.

            // Resolve the physical file path for the cached object.
            var path = GetFilePath(key, Container);

            try
            {
                // Create or overwrite the cached file using asynchronous file access.
                await using var fileStream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                // Write raw binary content directly to the file.
                if (value is byte[] bytes)
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }

                // Copy stream content directly into the cached file.
                else if (value is Stream stream)
                {
                    await stream.CopyToAsync(fileStream);
                }

                // Serialize all other object types as JSON content.
                else
                {
                    var json = JsonSerializer.Serialize(value);

                    using var writer = new StreamWriter(fileStream);

                    await writer.WriteAsync(json ?? string.Empty);
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to save cache file {Path}", path);
            }
        }
    }
}

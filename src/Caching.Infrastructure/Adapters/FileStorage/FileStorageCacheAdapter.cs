using System;
using System.Text.Json;
using Caching.Core;
using Microsoft.Extensions.Logging;

namespace Caching.Infrastructure.Adapters.FileStorage
{
    public partial class FileStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;

        public string Container { get; }
        private readonly string _containerPath;

        /// <summary>
        /// In-storage cache adapter for local development.
        /// </summary>
        public FileStorageCacheAdapter(
            ILogger logger,
            string container, 
            FileStorageSettings cacheSettings)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            _logger = logger;

            Container = container.Trim();
            _containerPath = CreateContainer(Container);

            //fire on background thread
            _ = ClearCacheAsync(cacheSettings.AbsoluteExpiryHours);
        }


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


        private string GetFilePath(string key, string container)
        {
            var containerPath = CreateContainer(container);
            return Path.Combine(containerPath, key);
        }


        private async Task<T?> GetInternalAsync<T>(string key, string container)
        {
            try
            {
                var path = GetFilePath(key, container);
                if (!File.Exists(path)) return default;

                if (typeof(T) == typeof(byte[]))
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    return (T)(object)bytes;
                }

                if (typeof(T) == typeof(Stream))
                {
                    var stream = File.OpenRead(path);
                    return (T)(object)stream;
                }

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


        private Task<bool> ExistsInternalAsync(string key, string container)
        {
            var path = GetFilePath(key, container);
            return Task.FromResult(File.Exists(path));
        }


        public async Task<bool> ExistsAsync(string key)
        {
            return await ExistsInternalAsync(key, Container);
        }


        public async Task<bool> ExistsInContainerAsync(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return await ExistsInternalAsync(key, container);
        }



        public async Task<T?> GetAsync<T>(string key)
        {
            return await GetInternalAsync<T>(key, Container);
        }


        public async Task<T?> GetFromContainerAsync<T>(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "Cache container is required.",
                    nameof(container));

            return await GetInternalAsync<T>(key, container);
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var path = GetFilePath(key, Container);

            try
            {
                await using var fileStream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                if (value is byte[] bytes)
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }
                else if (value is Stream stream)
                {
                    await stream.CopyToAsync(fileStream);
                }
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


        public Task ClearCacheAsync(int absoluteExpirationHours)
        {
            var cutoff = DateTime.UtcNow.AddHours(-absoluteExpirationHours);
            var filePath = GetFilePath(string.Empty, Container); //root path

            foreach (var file in Directory.GetFiles(filePath))
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                    File.Delete(file);

            return Task.CompletedTask;
        }


        public void Dispose()
        {
            //nothing to do
        }

    }
}

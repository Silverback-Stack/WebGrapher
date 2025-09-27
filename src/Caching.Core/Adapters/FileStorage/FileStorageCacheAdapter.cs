using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Caching.Core.Adapters.FileStorage
{
    public partial class FileStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;

        public string Container { get; private set; }

        /// <summary>
        /// In-storage cache adapter for local development.
        /// </summary>
        public FileStorageCacheAdapter(
            string serviceName, 
            ILogger logger, 
            CacheSettings cacheSettings)
        {
            _logger = logger;

            Container = CreateContainer(serviceName, cacheSettings.FileStorage.ContainerName);

            //fire on background thread
            _ = ClearCacheAsync(cacheSettings.FileStorage.AbsoluteExpirationHours);
        }

        private string CreateContainer(string serviceName, string containerName)
        {
            var folderName = $"{containerName}-{serviceName}".ToLowerInvariant();

            var containerPath = Path.Combine(AppContext.BaseDirectory, folderName);

            try
            {
                Directory.CreateDirectory(containerPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to create cache container.");
                throw;
            }

            return containerPath;
        }

        private string GetFilePath(string key) => Path.Combine(Container, key);


        public async Task<bool> ExistsAsync(string key)
        {
            var path = GetFilePath(key);
            if (File.Exists(path)) return true;

            return false;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var path = GetFilePath(key);
                if (!File.Exists(path)) return default;

                if (typeof(T) == typeof(byte[]))
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    return (T)(object)bytes;
                }
                else if (typeof(T) == typeof(Stream))
                {
                    var stream = File.OpenRead(path);
                    return (T)(object)stream;
                }

                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to read file {key}");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var path = GetFilePath(key);

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
                _logger.LogWarning(ex, $"Unable to save cache file {path}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            var path = GetFilePath(key);

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to delete cache file {path}");
            }
        }

        public async Task ClearCacheAsync(int absoluteExpirationHours)
        {
            var cutoff = DateTime.UtcNow.AddHours(-absoluteExpirationHours);
            var filePath = GetFilePath(string.Empty); //root path

            foreach (var file in Directory.GetFiles(filePath))
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                    File.Delete(file);
        }

        public void Dispose()
        {
            //nothing to do
        }
    }
}

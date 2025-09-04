using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Caching.Core.Adapters.InStorage
{
    public partial class InStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly CacheSettings _cacheSettings;

        /// <summary>
        /// In-storage cache adapter for local development.
        /// </summary>
        public InStorageCacheAdapter(string serviceName, ILogger logger, CacheSettings cacheSettings)
        {
            _logger = logger;
            _cacheSettings = cacheSettings;
            Container = CreateContainer(serviceName, _cacheSettings.InStorage.ContainerName);

            ClearCacheAsync();
        }

        public string Container { get; private set; }

        private string CreateContainer(string serviceName, string containerName)
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, containerName);
            var containerPath = Path.Combine(basePath, serviceName);

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

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            //no need to implement expiration on local delevopement environment
            //instead using absolute expiry to clear files
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
            DeleteFile(path);
        }

        private void DeleteFile(string path)
        {
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

        private void ClearCacheAsync()
        {
            var cutoff = DateTime.UtcNow.AddHours(-_cacheSettings.InStorage.AbsoluteExpirationHours);
            var filePath = GetFilePath(string.Empty);

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

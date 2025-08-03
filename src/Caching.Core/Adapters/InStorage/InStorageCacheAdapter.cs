using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Caching.Core.Adapters.InStorage
{
    public partial class InStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly string _containerPath;

        private const int DEFAULT_MAX_ABSOLUTE_EXPIRATION_DAYS = 30;

        private const int CACHE_CLEANUP_INTERVAL_MINUTES = 5;
        private static DateTimeOffset _lastCleanup = DateTimeOffset.MinValue;
        private static readonly object _cleanupLock = new();
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(CACHE_CLEANUP_INTERVAL_MINUTES);

        private const string CACHE_CONTAINER = "storage.cache";
        private const string CACHE_SUFFIX_DATA = ".data.cache";
        private const string CACHE_SUFFIX_META = ".meta.cache";

        /// <summary>
        /// In-storage cache adapter for local development.
        /// </summary>
        public InStorageCacheAdapter(string serviceName, ILogger logger)
        {
            _logger = logger;
            _containerPath = CreateContainer(serviceName);

            RunCacheMaintenance();
        }

        private string CreateContainer(string serviceName)
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, CACHE_CONTAINER);
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

        private string GetFilePath(string key) => Path.Combine(_containerPath, $"{key}{CACHE_SUFFIX_DATA}");

        private string GetMetadataPath(string key) => Path.Combine(_containerPath, $"{key}{CACHE_SUFFIX_META}");

        public async Task<bool> ExistsAsync(string key)
        {
            var path = GetFilePath(key);
            return File.Exists(path);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var path = GetFilePath(key);
            if (!File.Exists(path)) return default;

            CacheMetadata? metadata = null;
            var metaPath = GetMetadataPath(key);
            if (File.Exists(metaPath))
            {
                var metaJson = await File.ReadAllTextAsync(metaPath);
                metadata = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

                if (metadata?.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    _logger.LogDebug($"Cache entry expired for key: {key}");
                    return default;
                }
            }

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
            RunCacheMaintenance();

            var path = GetFilePath(key);

            var meta = new CacheMetadata
            {
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiration.HasValue
                    ? DateTimeOffset.UtcNow.Add(expiration.Value)
                    : DateTimeOffset.UtcNow.AddDays(DEFAULT_MAX_ABSOLUTE_EXPIRATION_DAYS)
            };

            var metaJson = JsonSerializer.Serialize(meta);
            await WriteTextFileAsync(GetMetadataPath(key), metaJson);

            if (value is byte[] bytes)
            {
                await File.WriteAllBytesAsync(path, bytes);
            }
            else if(value is Stream stream)
            {
                using var destination = File.OpenWrite(path);
                await stream.CopyToAsync(destination);
            }
            else
            {
                var json = JsonSerializer.Serialize(value);
                await WriteTextFileAsync(path, json);
            }
        }

        public async Task RemoveAsync(string key)
        {
            var path = GetFilePath(key);
            DeleteFile(path);

            var metaPath = GetMetadataPath(key);
            DeleteFile(metaPath);

        }

        private async Task WriteTextFileAsync(string path, string? contents)
        {
            try
            {
                await File.WriteAllTextAsync(path, contents);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to save cache file {path}");
            }
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

        private void RunCacheMaintenance()
        {
            if (DateTimeOffset.UtcNow - _lastCleanup > _cleanupInterval)
            {
                lock (_cleanupLock)
                {
                    if (DateTimeOffset.UtcNow - _lastCleanup > _cleanupInterval) // Double-check inside lock
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ClearCacheAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error during opportunistic cache cleanup");
                            }
                        });

                        _lastCleanup = DateTimeOffset.UtcNow;
                    }
                }
            }
        }

        private async Task ClearCacheAsync()
        {
            foreach (var metaFile in Directory.GetFiles(_containerPath, $"*{CACHE_SUFFIX_META}"))
            {
                try
                {
                    var key = Path.GetFileNameWithoutExtension(metaFile).Replace(CACHE_SUFFIX_META, "");
                    var json = await File.ReadAllTextAsync(metaFile);
                    var metadata = JsonSerializer.Deserialize<CacheMetadata>(json);
                    if (metadata?.ExpiresAt < DateTimeOffset.UtcNow)
                    {
                        await RemoveAsync(key);
                        _logger.LogInformation("Expired cache cleared: {Key}", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to process cache metadata file: {metaFile}");
                }
            }
        }

        public void Dispose()
        {
            //nothing to do
        }
    }
}

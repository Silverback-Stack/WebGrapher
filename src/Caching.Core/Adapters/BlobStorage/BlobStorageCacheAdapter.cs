using System;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace Caching.Core.Adapters.BlobStorage
{
    public class BlobStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly BlobContainerClient _containerClient;

        public string Container { get; private set; }

        public BlobStorageCacheAdapter(
            string serviceName,
            ILogger logger,
            CacheSettings cacheSettings)
        {
            _logger = logger;

            Container = $"{cacheSettings.BlobStorage.ContainerName}-{serviceName}".ToLowerInvariant();

            var blobServiceClient = new BlobServiceClient(cacheSettings.BlobStorage.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(Container);

            _containerClient.CreateIfNotExists(PublicAccessType.None);

            //fire on background thread
            _ = ClearCacheAsync(cacheSettings.BlobStorage.AbsoluteExpiryHours);
        }

        private BlobClient GetBlobClient(string key) {
            //Normalize slashes from file path to blob path
            var blobName = key.Replace("\\", "/");

            var containerName = string.Empty;

            if (blobName.Contains("/"))
            {
                var parts = blobName.Split('/', 2); // split on first slash
                containerName = parts[0];
                blobName = parts[1];

                // Get service client from existing container client
                var blobServiceClient = _containerClient.GetParentBlobServiceClient();
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                return containerClient.GetBlobClient(blobName);
            }

            // Default to the adapter's container
            return _containerClient.GetBlobClient(blobName);
        }
            

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var blob = GetBlobClient(key);
                return await blob.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error checking existence of blob {key}");
                return false;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var blob = GetBlobClient(key);
            if (!await blob.ExistsAsync()) return default;

            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    using var ms = new MemoryStream();
                    await blob.DownloadToAsync(ms);
                    return (T)(object)ms.ToArray();
                }
                else if (typeof(T) == typeof(Stream))
                {
                    var response = await blob.DownloadAsync();
                    return (T)(object)response.Value.Content;
                }
                else
                {
                    var response = await blob.DownloadContentAsync();
                    var json = response.Value.Content.ToString();
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to read blob {key}");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var blob = GetBlobClient(key);

                if (value is byte[] bytes)
                {
                    using var ms = new MemoryStream(bytes);
                    await blob.UploadAsync(ms, overwrite: true);
                }
                else if (value is Stream stream)
                {
                    await blob.UploadAsync(stream, overwrite: true);
                }
                else
                {
                    var json = JsonSerializer.Serialize(value);
                    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                    await blob.UploadAsync(ms, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to upload blob {key}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var blob = GetBlobClient(key);
                await blob.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to delete blob {key}");
            }
        }

        public async Task ClearCacheAsync(int absoluteExpirationHours)
        {
            var cutoff = DateTime.UtcNow.AddHours(-absoluteExpirationHours);

            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                if (blobItem.Properties.LastModified.HasValue &&
                    blobItem.Properties.LastModified.Value.UtcDateTime < cutoff)
                {
                    try
                    {
                        await _containerClient.DeleteBlobIfExistsAsync(blobItem.Name);
                        _logger.LogDebug($"Deleted expired cache blob {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Unable to delete expired cache blob {blobItem.Name}");
                    }
                }
            }
        }


        public void Dispose()
        {
            // Nothing to dispose for BlobContainerClient
        }
    }
}

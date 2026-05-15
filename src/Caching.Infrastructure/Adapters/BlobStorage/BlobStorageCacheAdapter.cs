using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Caching.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace Caching.Infrastructure.Adapters.BlobStorage
{
    public class BlobStorageCacheAdapter : ICache
    {
        private readonly ILogger _logger;
        private readonly BlobContainerClient _containerClient;

        public string Container { get; }
        private readonly string _containerName;

        public BlobStorageCacheAdapter(
            ILogger logger,
            string container,
            BlobStorageSettings cacheSettings)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "A cache container is required.",
                    nameof(container));

            if (string.IsNullOrWhiteSpace(cacheSettings.ConnectionString))
                throw new ArgumentException(
                    "Connection string is required.",
                    nameof(cacheSettings.ConnectionString));

            _logger = logger;

            Container = container.Trim();
            _containerName = CreateContainer(Container);

            var blobServiceClient = new BlobServiceClient(cacheSettings.ConnectionString);

            _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            _containerClient.CreateIfNotExists(PublicAccessType.None);

            //fire on background thread
            _ = ClearCacheAsync(cacheSettings.AbsoluteExpiryHours);
        }

        private static string CreateContainer(string container)
        {
            // Azure Blob container names must:
            // - be lowercase
            // - contain only letters, numbers and hyphens
            // - be between 3 and 63 characters
            // - not contain consecutive hyphens

            // Normalize common separators
            var name = container
                .Trim()
                .ToLowerInvariant()
                .Replace(".", "-")
                .Replace("_", "-");

            // Replace unsupported characters with hyphens
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-z0-9-]", "-");

            // Collapse repeated hyphens and trim edge hyphens
            name = System.Text.RegularExpressions.Regex.Replace(name, @"-+", "-").Trim('-');

            // Azure requires a minimum length of 3 characters
            if (name.Length < 3)
                name = name.PadRight(3, '0');

            // Azure limits container names to 63 characters
            if (name.Length > 63)
                name = name[..63].Trim('-');

            return name;
        }


        private BlobClient GetBlobClient(string key, string container)
        {
            var containerName = CreateContainer(container);

            //Normalize slashes from file path to blob path
            var blobName = key.Replace("\\", "/");

            // Get service client from existing container client
            var blobServiceClient = _containerClient.GetParentBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            return containerClient.GetBlobClient(blobName);
        }


        private async Task<T?> GetInternal<T>(string key, string container)
        {
            var blob = GetBlobClient(key, container);

            if (!await blob.ExistsAsync())
                return default;

            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    using var ms = new MemoryStream();
                    await blob.DownloadToAsync(ms);
                    return (T)(object)ms.ToArray();
                }

                if (typeof(T) == typeof(Stream))
                {
                    var response = await blob.DownloadAsync();
                    return (T)(object)response.Value.Content;
                }

                var content = await blob.DownloadContentAsync();
                var json = content.Value.Content.ToString();

                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to read blob {Key} from container {Container}",
                    key,
                    container);

                return default;
            }
        }


        private async Task<bool> ExistsInternalAsync(string key, string container)
        {
            try
            {
                var blob = GetBlobClient(key, container);
                return await blob.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, 
                    "Error checking existence of blob {Key} from container {Container}",
                    key,
                    container);
                return false;
            }
        }


        public async Task<T?> GetAsync<T>(string key)
        {
            return await GetInternal<T>(key, Container);
        }


        public async Task<T?> GetFromContainerAsync<T>(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "A cache container is required.",
                    nameof(container));

            return await GetInternal<T>(key, container);
        }


        public async Task<bool> ExistsAsync(string key)
        {
            return await ExistsInternalAsync(key, Container);
        }


        public async Task<bool> ExistsInContainerAsync(string key, string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException(
                    "A cache container is required.",
                    nameof(container));

            return await ExistsInternalAsync(key, container);
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var blob = GetBlobClient(key, Container);

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
                _logger.LogWarning(ex, "Unable to upload blob {Key}", key);
            }
        }


        public async Task RemoveAsync(string key)
        {
            try
            {
                var blob = GetBlobClient(key, Container);
                await blob.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to delete blob {Key}", key);
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
                        _logger.LogDebug("Deleted expired cache blob {Name}", blobItem.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to delete expired cache blob {Name}", blobItem.Name);
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

using System;
using System.Security.Cryptography;
using System.Text;

namespace Caching.Core.Helpers
{
    public static class CacheKeyHelper
    {
        /// <summary>
        /// Generates a 64-character hashed cache key from the composite key.
        /// </summary>
        public static string ComputeCacheKey(string compositeKey)
            => ComputeHash(compositeKey);

        private static string ComputeHash(string compositeKey)
        {
            // Return empty if key is missing.
            if (string.IsNullOrWhiteSpace(compositeKey))
                return string.Empty;

            // Create a SHA256 instance to compute the hash.
            using var sha = SHA256.Create();

            // Hash the key to produce a fixed-length value:
            // SHA-256 always produces a 32-byte hash regardless of input size.
            var hashBytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(compositeKey.Trim()));

            // Convert hash bytes to lowercase hex 64-char string.
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

    }
}

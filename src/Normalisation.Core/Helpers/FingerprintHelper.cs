using System;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace Normalisation.Core.Helpers
{
    public static class FingerprintHelper
    {
        /// <summary>
        /// Generates a 64-character fingerprint representing the normalised page data.
        /// </summary>
        public static string ComputeFingerprint(PageDataStandardised pageData)
        {
            var data = new
            {
                pageData.Title,
                pageData.Summary,
                pageData.Keywords,
                Tags = pageData.Tags?.OrderBy(x => x),
                Links = pageData.Links?
                    .Select(x => x.AbsoluteUri)
                    .OrderBy(x => x),
                pageData.ImageUrl?.AbsoluteUri,
                pageData.ImageCors,
                pageData.LanguageIso3
            };

            var json = JsonSerializer.Serialize(data);

            return ComputeHash(json);
        }


        /// <summary>
        /// Computes a SHA-256 hash and returns it as a lowercase hexadecimal string.
        /// </summary>
        private static string ComputeHash(string value)
        {
            // Return empty if key is missing.
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Create a SHA256 instance to compute the hash.
            using var sha = SHA256.Create();

            // Hash the key to produce a fixed-length value:
            // SHA-256 always produces a 32-byte hash regardless of input size.
            var hashBytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(value.Trim()));

            // Convert hash bytes to lowercase hex 64-char string.
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}

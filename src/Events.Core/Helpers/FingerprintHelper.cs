using System;
using System.Text;
using System.Security.Cryptography;

namespace Events.Core.Helpers
{
    public class FingerprintHelper
    {
        /// <summary>
        /// Generate a fingerprint for determining unique signature of content.
        /// </summary>
        public static string ComputeFingerprint(string compositeKey)
            => ComputeHash(compositeKey);

        /// <summary>
        /// Computes a SHA-256 hash of the provided key and returns it as a lowercase hex string.
        /// </summary>
        private static string ComputeHash(string compositeKey)
        {
            if (string.IsNullOrWhiteSpace(compositeKey))
                return string.Empty;

            using var sha = SHA256.Create();

            //limit length of key:
            //SHA-256 hash output → 256 bits = 32 bytes
            //Base64 encoding of 32 bytes → 44-character string
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(compositeKey.Trim()));

            return Convert.ToHexString(hashBytes).ToLowerInvariant(); // 64-char string
        }
    }
}

using System;
using System.Security.Cryptography;
using System.Text;

namespace Caching.Core.Helpers
{
    public static class CacheKeyHelper
    {
        public static string GetHashCode(string compositeKey)
        {
            compositeKey = compositeKey.Trim();

            //limit length of key:
            //SHA-256 hash output → 256 bits = 32 bytes
            //Base64 encoding of 32 bytes → 44-character string
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(compositeKey));

            return BitConverter.ToString(hash)
                .Replace("-", ""); //make human readable by replacing hyphens eg: 6A-FC-1D-E9-
        }

    }
}

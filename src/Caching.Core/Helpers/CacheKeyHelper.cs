using System.Text;

namespace Caching.Core.Helpers
{
    public static class CacheKeyHelper
    {
        public static string Generate(string url, string? userAgent, string? userAccepts)
        {
            if (userAgent == null) userAgent = string.Empty;
            if (userAccepts == null) userAccepts = string.Empty;

            //possible length of key issue for cache providers:
            var composite = $"{url}_{userAgent}_{userAccepts}";

            //limit length of key:
            //SHA-256 hash output → 256 bits = 32 bytes
            //Base64 encoding of 32 bytes → 44-character string
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                .ComputeHash(Encoding.UTF8.GetBytes(composite))
            );
        }
    }
}

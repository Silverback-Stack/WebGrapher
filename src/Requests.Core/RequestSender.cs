using System;
using System.Net;
using System.Text;
using Caching.Core;
using Logging.Core;

namespace Requests.Core
{
    public class RequestSender : IRequestSender
    {
        private readonly ILogger _logger;
        private readonly ICache _cache;
        private readonly IHttpRequester _httpRequester;
        private readonly IRequestTransformer _requestTransformer;

        private const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        private const string DEFAULT_CLIENT_ACCEPTS = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";
        private const int MAX_ABSOLUTE_EXPIRY_MINUTES = 5;

        public RequestSender(ILogger logger, ICache cache, IHttpRequester httpRequester, IRequestTransformer requestTransformer)
        {
            _logger = logger;
            _cache = cache;
            _httpRequester = httpRequester;
            _requestTransformer = requestTransformer;
        }

        public async Task<RequestResponseItem?> GetStringAsync(Uri url, CancellationToken cancellationToken = default) =>
            await GetStringAsync(url, userAgent: string.Empty, clientAccepts: string.Empty, contentMaxBytes: 0, cancellationToken);

        public async Task<RequestResponseItem?> GetStringAsync(
            Uri url, 
            string? userAgent, 
            string? clientAccepts, 
            int contentMaxBytes = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = DEFAULT_USER_AGENT;

            if (string.IsNullOrWhiteSpace(clientAccepts))
                clientAccepts = DEFAULT_CLIENT_ACCEPTS;

            try
            {
                var cacheKey = BuildCacheKey(url, userAgent, clientAccepts);
                var cachedItem = await _cache.GetAsync<RequestResponseItem>(cacheKey);
                if (cachedItem != null) return cachedItem;

                var response = await _httpRequester.GetAsync(url, userAgent, clientAccepts, cancellationToken);
                var responseItem = await _requestTransformer.TransformAsync(response, clientAccepts, contentMaxBytes, cancellationToken);

                _logger.LogInformation($"Get: {url.AbsoluteUri} Status Code: {responseItem.StatusCode}");

                await _cache.SetAsync(
                    cacheKey, 
                    responseItem, 
                    GetCacheDuration(responseItem?.Expires, MAX_ABSOLUTE_EXPIRY_MINUTES));

                return responseItem;
            }
            catch (Exception ex)
            {
                //CATCH ALL:
                //OperationCanceledException: cancellation token is triggered
                //HttpRequestException: Connection issues, DNS failures, SSL errors
                //TaskCanceledException: Timeouts
                //OperationCanceledException: Explicit cancellation via CancellationToken
                //InvalidOperationException: Misconfigured HttpClient
                _logger.LogError($"Get: {url.AbsoluteUri} Exception: {ex.Message} InnerException: {ex.InnerException?.Message}");
            }

            return null;
        }

        public static string BuildCacheKey(Uri uri, string userAgent, string acceptHeader)
        {
            //possible length of key issue for cache providers:
            var composite = $"{uri}_{userAgent}_{acceptHeader}";

            //limit length of key:
            //SHA-256 hash output → 256 bits = 32 bytes
            //Base64 encoding of 32 bytes → 44-character string
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                .ComputeHash(Encoding.UTF8.GetBytes(composite))
            );
        }

        public static TimeSpan? GetCacheDuration(
            DateTimeOffset? expires, 
            int expiryMinutes = MAX_ABSOLUTE_EXPIRY_MINUTES)
        {
            //honor server expires header
            if (!expires.HasValue) return null;

            var cacheDuration = expires.Value - DateTimeOffset.UtcNow;
            var maxDuration = TimeSpan.FromMinutes(expiryMinutes);

            //override servers expires header if greater than our own
            if (cacheDuration > maxDuration)
                cacheDuration = maxDuration;

            return cacheDuration > TimeSpan.Zero ? cacheDuration : null;
        }
    }
}

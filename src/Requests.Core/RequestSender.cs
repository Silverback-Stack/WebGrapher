using System;
using System.Net;
using System.Text;
using Caching.Core;
using Caching.Core.Helpers;
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

        public RequestSender(ILogger logger, ICache cache, IHttpRequester httpRequester, IRequestTransformer requestTransformer)
        {
            _logger = logger;
            _cache = cache;
            _httpRequester = httpRequester;
            _requestTransformer = requestTransformer;
        }

        public async Task<ResponseEnvelope<ResponseItem>?> GetStringAsync(Uri url, CancellationToken cancellationToken = default) =>
            await GetStringAsync(url, userAgent: string.Empty, userAccepts: string.Empty, contentMaxBytes: 0, cancellationToken);

        public async Task<ResponseEnvelope<ResponseItem>?> GetStringAsync(
            Uri url, 
            string? userAgent, 
            string? userAccepts, 
            int contentMaxBytes = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = DEFAULT_USER_AGENT;

            if (string.IsNullOrWhiteSpace(userAccepts))
                userAccepts = DEFAULT_CLIENT_ACCEPTS;

            try
            {
                var cacheKey = CacheKeyHelper.Generate(url.AbsoluteUri, userAgent, userAccepts);
                var cachedItem = await _cache.GetAsync<ResponseItem>(cacheKey);
                if (cachedItem != null)
                {
                    _logger.LogDebug($"Get: {url.AbsoluteUri} Status: Cached");
                    return new ResponseEnvelope<ResponseItem>(cachedItem, IsFromCache: true);
                }
                    
                var response = await _httpRequester.GetAsync(url, userAgent, userAccepts, cancellationToken);
                var responseItem = await _requestTransformer.TransformAsync(url, response, userAccepts, contentMaxBytes, cancellationToken);

                _logger.LogDebug($"Get: {url.AbsoluteUri} Status: {responseItem.StatusCode}");

                await _cache.SetAsync(
                    cacheKey, 
                    responseItem, 
                    CacheDurationHelper.Clamp(responseItem?.Expires));

                return new ResponseEnvelope<ResponseItem>(responseItem, IsFromCache: false);
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

    }
}

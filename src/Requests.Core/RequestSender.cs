using System;
using Caching.Core;
using Caching.Core.Helpers;
using Microsoft.Extensions.Logging;

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

        public async Task<HttpResponseEnvelope?> GetStringAsync(Uri url, CancellationToken cancellationToken = default) =>
            await GetStringAsync(url, userAgent: string.Empty, userAccepts: string.Empty, contentMaxBytes: 0, cancellationToken);

        public async Task<HttpResponseEnvelope?> GetStringAsync(
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
                //request cached data:
                var key = CacheKeyHelper.Generate(url.AbsoluteUri, userAgent, userAccepts);
                var keyMeta = CacheKeyHelper.AppendSuffix(key, CacheKeyType.BlobMeta);
                var keyData = CacheKeyHelper.AppendSuffix(key, CacheKeyType.BlobData);

                var getMetaTask = _cache.GetAsync<HttpResponseMetadata>(keyMeta);
                var getDataTask = _cache.GetAsync<HttpResponseData>(keyData);
                await Task.WhenAll(getMetaTask, getDataTask);

                var cachedMeta = getMetaTask.Result;
                var cachedData = getDataTask.Result;


                if (cachedMeta != null && cachedData != null)
                {
                    _logger.LogDebug($"Get request for {url.AbsoluteUri} returned cached item.");
                    return new HttpResponseEnvelope
                    {
                        Metadata = cachedMeta,
                        Data = cachedData,
                        IsFromCache = true
                    };
                }

                //request live data:
                var httpResponseMessage = await _httpRequester.GetAsync(url, userAgent, userAccepts, cancellationToken);
                var httpResponseEnvelope = await _requestTransformer.TransformAsync(url, httpResponseMessage, userAccepts, key, contentMaxBytes, cancellationToken);

                _logger.LogDebug($"Get request for {url.AbsoluteUri} returned status code {httpResponseEnvelope.Metadata.StatusCode}");

                var expiry = CacheDurationHelper.Clamp(httpResponseEnvelope.Metadata?.Expires);
                var setMetaTask = _cache.SetAsync(keyMeta, httpResponseEnvelope.Metadata, expiry);
                var setDataTask = _cache.SetAsync(keyData, httpResponseEnvelope.Data, expiry);
                await Task.WhenAll(setMetaTask, setDataTask);

                httpResponseEnvelope.IsFromCache = false;
                return httpResponseEnvelope;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get request for {url.AbsoluteUri} threw an exception.");
            }

            return null;
        }

    }
}

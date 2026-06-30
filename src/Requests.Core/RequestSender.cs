using Caching.Core;
using Caching.Core.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Requests.Core
{
    public class RequestSender : IRequestSender
    {
        private readonly ILogger _logger;
        private readonly ICache _metaCache;
        private readonly ICache _blobCache;
        private readonly IHttpTransport _httpTransport;
        private readonly RequestSenderSettings _requestSenderSettings;

        private readonly string _groupKey;

        public RequestSender(
            ILogger logger, 
            ICache metaCache, 
            ICache blobCache, 
            IHttpTransport httpTransport, 
            RequestSenderSettings requestSenderSettings)
        {
            _requestSenderSettings = requestSenderSettings;
            _logger = logger;
            _metaCache = metaCache;
            _blobCache = blobCache;
            _httpTransport = httpTransport;

            _groupKey = ResolveGroupKey();
        }


        public string GroupKey => _groupKey;


        /// <summary>
        /// Resolve the group key for this Request Sender.
        /// If a GroupKey is configured, Request Senders using the same value belong to the same group.
        /// If no value is configured, generate a unique key so this instance is grouped independently.
        /// </summary>
        private string ResolveGroupKey()
        {
            if (!string.IsNullOrWhiteSpace(_requestSenderSettings.GroupKey))
                return _requestSenderSettings.GroupKey;

            return Guid.NewGuid().ToString("N");
        }


        /// <summary>
        /// Fetches content from the specified URL, using cached data when available,
        /// otherwise sending an HTTP request and caching the result.
        /// </summary>
        public async Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent,
            string userAccepts,
            int contentMaxBytes = 0,
            string compositeKey = "",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url.AbsoluteUri)) 
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(userAgent)) 
                throw new ArgumentNullException(nameof(userAgent));

            if (string.IsNullOrWhiteSpace(userAccepts))
                throw new ArgumentNullException(nameof(userAccepts));

            if (string.IsNullOrEmpty(compositeKey))
                compositeKey = $"{url}|{userAgent}|{userAccepts}";

            try
            {
                // fetch cached data:
                string key = CacheKeyHelper.ComputeCacheKey(compositeKey);

                var cachedResponse = await TryGetCachedResponseAsync(key, url);

                if (cachedResponse is not null)
                    return cachedResponse;


                // fetch fresh data:
                var responseEnvelope = await _httpTransport.GetAsync(
                    url,
                    userAgent,
                    userAccepts,
                    contentMaxBytes: contentMaxBytes,
                    cancellationToken: cancellationToken);

                if (responseEnvelope is null)
                    return null;

                responseEnvelope.Cache = new CacheInfo
                {
                    IsFromCache = false,
                    Key = key,
                    Container = _blobCache.Container
                };

                responseEnvelope.RequestSenderGroupKey = _groupKey;

                _logger.LogDebug(
                    "Fetch request for {AbsoluteUri} returned status code {StatusCode}",
                    url.AbsoluteUri, responseEnvelope.Metadata.StatusCode);

                await CacheIfEligibleAsync(key, responseEnvelope);

                return responseEnvelope;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred (not user-requested cancellation)
                _logger.LogWarning(ex, $"Request to {url.AbsoluteUri} timed out.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, $"Request to {url.AbsoluteUri} timed out: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                // Server not running / connection refused
                _logger.LogWarning(ex, $"Could not connect to {url.AbsoluteUri}.");
            }
            catch (Exception ex)
            {
                // All other unexpected exceptions
                _logger.LogError(ex, $"Get request for {url.AbsoluteUri} threw an exception.");
            }

            return null;
        }


        /// <summary>
        /// Attempts to retrieve a cached response using the specified key.
        /// </summary>
        private async Task<HttpResponseEnvelope?> TryGetCachedResponseAsync(string key, Uri url)
        {
            var metaData = await _metaCache.GetAsync<HttpResponseMetadata>(key);

            if (metaData is null) 
                return null;

            var blobData = await _blobCache.GetAsync<byte[]>(key);

            if (blobData is null)
                return null;

            _logger.LogDebug("Fetch request for {AbsoluteUri} returned cached item.", url.AbsoluteUri);

            return new HttpResponseEnvelope
            {
                Metadata = metaData,
                Data = new HttpResponseData
                {
                    Payload = blobData
                },
                Cache = new CacheInfo
                {
                    IsFromCache = true,
                    Key = key,
                    Container = _blobCache.Container
                },
                RequestSenderGroupKey = _groupKey
            };
        }


        /// <summary>
        /// Caches the response metadata and payload if the response is eligible for caching.
        /// </summary>
        private async Task CacheIfEligibleAsync(
            string key, HttpResponseEnvelope responseEnvelope)
        {
            if (!IsCacheable(responseEnvelope.Metadata.StatusCode))
                return;

            var expiry = CacheDurationHelper.Clamp(
                responseEnvelope.Metadata.Expires,
                _requestSenderSettings.CacheMinAbsoluteExpiryMinutes,
                _requestSenderSettings.CacheMaxAbsoluteExpiryMinutes);

            var metaTask = _metaCache.SetAsync(key, responseEnvelope.Metadata, expiry);

            var blobTask = _blobCache.SetAsync(key, responseEnvelope.Data?.Payload, expiry);

            await Task.WhenAll(metaTask, blobTask);
        }


        /// <summary>
        /// Determines whether a response status code is eligible for request-result caching.
        /// Used to avoid repeatedly requesting resources that are unlikely to change immediately.
        /// </summary>
        private static bool IsCacheable(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.Gone:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.NonAuthoritativeInformation:
                case HttpStatusCode.MultipleChoices:
                case HttpStatusCode.MovedPermanently:
                case HttpStatusCode.MethodNotAllowed:
                case HttpStatusCode.NotImplemented:
                    return true;

                default:
                    return false;
            }
        }

    }
}

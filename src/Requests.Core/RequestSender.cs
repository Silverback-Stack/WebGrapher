using System;
using System.Net;
using Caching.Core;
using Caching.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Requests.Core
{
    public class RequestSender : IRequestSender
    {
        private readonly ILogger _logger;
        private readonly ICache _metaCache;
        private readonly ICache _blobCache;
        private readonly IHttpRequester _httpRequester;
        private readonly IRequestTransformer _requestTransformer;

        public RequestSender(
            ILogger logger, 
            ICache metaCache, 
            ICache blobCache, 
            IHttpRequester httpRequester, 
            IRequestTransformer requestTransformer)
        {
            _logger = logger;
            _metaCache = metaCache;
            _blobCache = blobCache;
            _httpRequester = httpRequester;
            _requestTransformer = requestTransformer;
        }


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
            {
                compositeKey = $"{url}|{userAgent}|{userAccepts}";
            }

            try
            {
                //fetch cached data:
                string key = CacheKeyHelper.GetHashCode(compositeKey);

                var metaData = await _metaCache.GetAsync<HttpResponseMetadata>(key);

                if (metaData is not null)
                {
                    var blobData = await _blobCache.GetAsync<byte[]>(key);
                    if (blobData is not null)
                    {
                        _logger.LogDebug($"Fetch request for {url.AbsoluteUri} returned cached item.");

                        return new HttpResponseEnvelope
                        {
                            Metadata = metaData,
                            Data = new HttpResponseData
                            {
                                Payload = blobData
                            },
                            IsFromCache = true
                        };
                    }
                }

                //fetch fresh data:
                var responseMessage = await _httpRequester
                    .GetAsync(url, userAgent, userAccepts, cancellationToken);

                var responseEnvelope = await _requestTransformer
                    .TransformAsync(url, responseMessage, userAccepts, blobId: key, blobContainer: _blobCache.Container, contentMaxBytes, cancellationToken);

                _logger.LogDebug($"Fetch request for {url.AbsoluteUri} returned status code {responseEnvelope.Metadata.StatusCode}");

                if (IsCachable(responseEnvelope.Metadata.StatusCode))
                {
                    var expiry = CacheDurationHelper.Clamp(responseEnvelope.Metadata?.Expires);
                    var metaTask = _metaCache.SetAsync(key, responseEnvelope.Metadata, expiry);
                    var blobTask = _blobCache.SetAsync(key, responseEnvelope.Data?.Payload, expiry);
                    await Task.WhenAll(metaTask, blobTask);
                }

                responseEnvelope.IsFromCache = false;
                return responseEnvelope;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get request for {url.AbsoluteUri} threw an exception.");
            }

            return null;
        }

        private bool IsCachable(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NonAuthoritativeInformation:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.PartialContent:
                case HttpStatusCode.MultipleChoices:
                case HttpStatusCode.MovedPermanently:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.MethodNotAllowed:
                case HttpStatusCode.Gone:
                case HttpStatusCode.NotImplemented:
                    return true;

                default:
                    return false;
            }
        }

    }
}

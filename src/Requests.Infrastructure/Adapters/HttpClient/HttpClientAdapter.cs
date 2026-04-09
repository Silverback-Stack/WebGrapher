using Requests.Core;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace Requests.Infrastructure.Adapters.HttpClient
{
    public class HttpClientAdapter : IHttpTransport
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly HttpClientSettings _httpClientSettings;

        public HttpClientAdapter(
            System.Net.Http.HttpClient httpClient, 
            HttpClientSettings httpClientSettings)
        {
            _httpClient = httpClient;
            _httpClientSettings = httpClientSettings;
        }

        /// <summary>
        /// Sends an HTTP GET request and returns the response as a response envelope.
        /// </summary>
        public async Task<HttpResponseEnvelope?> GetAsync(
            Uri uri,
            string userAgent,
            string userAccepts,
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default)
        {
            using var request = CreateRequest(uri, userAgent, userAccepts);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var metadata = CreateMetadata(uri, response, userAccepts);

            var payload = await ReadPayloadIfSuccessfulAsync(
                response, metadata.StatusCode, contentMaxBytes, cancellationToken);

            return new HttpResponseEnvelope
            {
                Metadata = metadata,
                Data = new HttpResponseData
                {
                    Payload = payload
                },
                Cache = new CacheInfo
                {
                    IsFromCache = false
                }
            };
        }


        /// <summary>
        /// Creates an HTTP GET request with the required request headers.
        /// </summary>
        private static HttpRequestMessage CreateRequest(
            Uri uri,
            string userAgent,
            string userAccepts)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(userAgent);
            request.Headers.Accept.ParseAdd(userAccepts);
            return request;
        }


        /// <summary>
        /// Creates response metadata from the HTTP response.
        /// </summary>
        private HttpResponseMetadata CreateMetadata(
            Uri originalUrl,
            HttpResponseMessage response,
            string userAccepts)
        {
            var requestUrl = response.RequestMessage?.RequestUri ?? originalUrl;

            var contentType = ResolveContentType(response.Content);
      
            var encoding = ResolveEncoding(response.Content);

            var lastModified = response.Content?.Headers?.LastModified?.UtcDateTime
                ?? DateTimeOffset.UtcNow; //default to current time to support caching behavior

            var expires = response.Content?.Headers?.Expires;

            var retryAfter = GetRetryAfterOffset(response.StatusCode, response.Headers?.RetryAfter);

            var statusCode = response.StatusCode;
            if (!IsContentAcceptable(contentType, userAccepts))
                statusCode = HttpStatusCode.NotAcceptable;

            return new HttpResponseMetadata
            {
                OriginalUrl = originalUrl,
                Url = requestUrl,
                StatusCode = statusCode,
                LastModified = lastModified,
                Expires = expires,
                RetryAfter = retryAfter,
                IsCorsAllowed = IsCorsAllowed(response.Headers),
                ContentType = contentType,
                Encoding = encoding
            };
        }


        /// <summary>
        /// Resolves the response content type, or returns a default value when unavailable.
        /// </summary>
        private static string ResolveContentType(HttpContent? content)
        {
            var defaultContentType = new ContentType("text/html");

            try
            {
                var rawContentType = content?.Headers?.ContentType?.ToString();
                var contentType = !string.IsNullOrWhiteSpace(rawContentType)
                    ? new ContentType(rawContentType)
                    : defaultContentType;

                return contentType.MediaType;
            }
            catch (FormatException)
            {
                return defaultContentType.MediaType;
            }
            catch (ArgumentNullException)
            {
                return defaultContentType.MediaType;
            }
        }


        /// <summary>
        /// Resolves the response encoding, or returns UTF-8 when unavailable.
        /// </summary>
        private static string ResolveEncoding(HttpContent? content)
        {
            var charset = content?.Headers?.ContentType?.CharSet?.Trim('"');

            try
            {
                var encoding = !string.IsNullOrWhiteSpace(charset)
                    ? Encoding.GetEncoding(charset)
                    : Encoding.UTF8;

                return encoding.WebName;
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8.WebName;
            }
        }


        /// <summary>
        /// Returns the Retry-After value as an absolute time.
        /// </summary>
        private DateTimeOffset? GetRetryAfterOffset(
            HttpStatusCode statusCode,
            RetryConditionHeaderValue? retryAfter)
        {
            if (retryAfter == null)
            {
                if (statusCode is HttpStatusCode.TooManyRequests
                    or HttpStatusCode.Forbidden
                    or HttpStatusCode.ServiceUnavailable)
                {
                    return DateTimeOffset.UtcNow.Add(
                        TimeSpan.FromMinutes(_httpClientSettings.RetryAfterFallbackMinutes));
                }

                return null;
            }

            if (retryAfter.Date.HasValue)
                return retryAfter.Date.Value;

            if (retryAfter.Delta.HasValue)
                return DateTimeOffset.UtcNow + retryAfter.Delta.Value;

            return null;
        }


        /// <summary>
        /// Determines whether the response content type matches the accepted media types.
        /// </summary>
        private static bool IsContentAcceptable(string? contentType, string userAccepts)
        {
            if (string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(userAccepts))
                return false;

            return userAccepts.Split(',')
                .Any(s => MediaTypeWithQualityHeaderValue.TryParse(s, out var v)
                    && (v.MediaType == "*/*"
                        || v.MediaType == contentType
                        || (v.MediaType!.EndsWith("/*")
                            && contentType.StartsWith(v.MediaType.Split('/')[0]))));
        }


        /// <summary>
        /// Reads the response payload when the request completed successfully.
        /// </summary>
        private static async Task<byte[]?> ReadPayloadIfSuccessfulAsync(
            HttpResponseMessage response,
            HttpStatusCode statusCode,
            int contentMaxBytes,
            CancellationToken cancellationToken)
        {
            if (statusCode != HttpStatusCode.OK)
                return null;

            return await ReadAsByteArrayAsync(response.Content, contentMaxBytes, cancellationToken);
        }



        /// <summary>
        /// Determines whether the response allows cross-origin access.
        /// </summary>
        private static bool IsCorsAllowed(HttpHeaders headers)
        {
            if (headers == null) return false;
            return headers.Contains("Access-Control-Allow-Origin");
        }



        /// <summary>
        /// Reads the response content as a byte array, optionally limiting the number of bytes read.
        /// </summary>
        private static async Task<byte[]?> ReadAsByteArrayAsync(
            HttpContent? content,
            int contentMaxBytes,
            CancellationToken cancellationToken = default)
        {
            if (content == null)
                return null;

            if (contentMaxBytes <= 0)
                return await content.ReadAsByteArrayAsync(cancellationToken);

            using var stream = await content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();

            var buffer = new byte[1024];
            int totalBytes = 0;

            while (totalBytes < contentMaxBytes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int readSize = Math.Min(buffer.Length, contentMaxBytes - totalBytes);
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readSize), cancellationToken);

                if (bytesRead == 0)
                    break;

                memoryStream.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }

            return memoryStream.ToArray();
        }
    }

}

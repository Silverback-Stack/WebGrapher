using Requests.Core;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Requests.Infrastructure.Adapters.HttpClient
{
    public class HttpClientAdapter : IHttpTransport
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly HttpClientSettings _httpClientSettings;

        private const string DefaultContentType = "text/html";
        private static readonly string DefaultEncoding = Encoding.UTF8.WebName;

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
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);
            ArgumentException.ThrowIfNullOrWhiteSpace(userAccepts);

            using var request = CreateRequest(uri, userAgent, userAccepts);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var metadata = CreateMetadata(uri, response, userAccepts);

            var payload = await ReadPayloadIfSuccessfulAsync(
                response, 
                metadata.StatusCode, 
                contentMaxBytes, 
                cancellationToken);

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
            request.Headers.UserAgent.TryParseAdd(userAgent);
            request.Headers.Accept.TryParseAdd(userAccepts);
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
            // Extract the final request URL (after redirects), fallback to original
            var requestUrl = response.RequestMessage?.RequestUri ?? originalUrl;

            // Resolve content type from response headers or use default
            var contentType = ResolveContentType(response.Content);

            // Resolve encoding from charset or use default
            var encoding = ResolveEncoding(response.Content);

            // Extract Last-Modified header if available, otherwise use current time
            var lastModified = response.Content?.Headers?.LastModified?.UtcDateTime
                ?? DateTimeOffset.UtcNow;

            // Extract Expires header from response if it exists
            var expires = response.Content?.Headers?.Expires;

            // Determine retry time from Retry-After header or use fallback
            var retryAfter = GetRetryAfterOffset(response.StatusCode, response.Headers?.RetryAfter);

            // Start with the actual response status code
            var statusCode = response.StatusCode;

            // Override if the content type is not accepted by the caller
            if (!IsContentAcceptable(contentType, userAccepts))
                statusCode = HttpStatusCode.NotAcceptable;

            // Indicates whether the response includes a CORS policy
            var hasCorsPolicy = HasCorsPolicy(response.Headers);

            return new HttpResponseMetadata
            {
                OriginalUrl = originalUrl,
                Url = requestUrl,
                StatusCode = statusCode,
                LastModified = lastModified,
                Expires = expires,
                RetryAfter = retryAfter,
                HasCorsPolicy = hasCorsPolicy,
                ContentType = contentType,
                Encoding = encoding
            };
        }


        /// <summary>
        /// Resolves the response content type, or returns the default type when unavailable.
        /// </summary>
        private static string ResolveContentType(HttpContent? content)
        {
            return content?.Headers?.ContentType?.MediaType ?? DefaultContentType;
        }


        /// <summary>
        /// Resolves the response encoding, or returns UTF-8 when unavailable.
        /// </summary>
        private static string ResolveEncoding(HttpContent? content)
        {
            var charset = content?.Headers?.ContentType?.CharSet?.Trim('"');

            if (string.IsNullOrWhiteSpace(charset))
                return DefaultEncoding;

            try
            {
                return Encoding.GetEncoding(charset).WebName;
            }
            catch (ArgumentException)
            {
                return DefaultEncoding;
            }
        }


        /// <summary>
        /// Returns the Retry-After value as an absolute time.
        /// </summary>
        private DateTimeOffset? GetRetryAfterOffset(
            HttpStatusCode statusCode,
            RetryConditionHeaderValue? retryAfter)
        {
            if (!SupportsRetryAfter(statusCode))
                return null;

            if (retryAfter?.Date.HasValue == true)
                return retryAfter.Date.Value;

            if (retryAfter?.Delta.HasValue == true)
                return DateTimeOffset.UtcNow + retryAfter.Delta.Value;

            // Apply fallback retry delay
            return DateTimeOffset.UtcNow.AddMinutes(
                _httpClientSettings.RetryAfterFallbackMinutes);
        }

        private static bool SupportsRetryAfter(HttpStatusCode statusCode)
        {
            return statusCode is
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.ServiceUnavailable;
        }


        /// <summary>
        /// Determines whether the response content type matches the accepted media types.
        /// </summary>
        private static bool IsContentAcceptable(string? contentType, string userAccepts)
        {
            if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(userAccepts))
                return false;

            // Returns true if the response content type is allowed by the Accept header.
            // Supports:
            // - Exact matches (e.g. text/html)
            // - Full wildcard (*/*) to accept any type
            // - Type wildcard (e.g. text/*) to accept a category of types
            return userAccepts.Split(',')
                .Any(s => MediaTypeWithQualityHeaderValue.TryParse(s, out var v)
                    && (v.MediaType == "*/*"
                        || v.MediaType == contentType
                        || (v.MediaType != null
                            && v.MediaType.EndsWith("/*")
                            && contentType.StartsWith($"{v.MediaType.Split('/')[0]}/"))));
        }


        /// <summary>
        /// Determines whether the response includes a CORS policy header.
        /// </summary>
        private static bool HasCorsPolicy(HttpHeaders? headers)
        {
            return headers?.Contains("Access-Control-Allow-Origin") == true;
        }


        /// <summary>
        /// Reads the response payload only for HTTP 200 (OK) responses.
        /// </summary>
        private static async Task<byte[]?> ReadPayloadIfSuccessfulAsync(
            HttpResponseMessage response,
            HttpStatusCode statusCode,
            int contentMaxBytes,
            CancellationToken cancellationToken)
        {
            if (statusCode != HttpStatusCode.OK)
                return null;

            return await ReadAsByteArrayAsync(
                response.Content, 
                contentMaxBytes, 
                cancellationToken);
        }


        /// <summary>
        /// Reads the response content as a byte array, optionally limiting the number of bytes read.
        /// </summary>
        private static async Task<byte[]?> ReadAsByteArrayAsync(
            HttpContent? content,
            int contentMaxBytes,
            CancellationToken cancellationToken = default)
        {
            // No content → nothing to read
            if (content == null)
                return null;

            // No limit specified → read entire response
            if (contentMaxBytes <= 0)
                return await content.ReadAsByteArrayAsync(cancellationToken);

            // Stream the response to avoid loading large payloads into memory
            using var stream = await content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();

            var buffer = new byte[1024];
            int totalBytes = 0;

            // Read until we reach the max byte limit or the stream ends
            while (totalBytes < contentMaxBytes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only read up to the remaining allowed bytes
                int readSize = Math.Min(buffer.Length, contentMaxBytes - totalBytes);
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readSize), cancellationToken);

                // End of stream
                if (bytesRead == 0)
                    break;

                // Append to memory buffer
                await memoryStream.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
                totalBytes += bytesRead;
            }

            // Return the buffered content as a byte array
            return memoryStream.ToArray();
        }
    }

}

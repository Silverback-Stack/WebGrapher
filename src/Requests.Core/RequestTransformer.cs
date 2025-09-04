using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace Requests.Core
{
    public class RequestTransformer : IRequestTransformer
    {
        private readonly RequestSenderSettings _requestSenderSettings;

        public RequestTransformer(RequestSenderSettings requestSenderSettings)
        {
            _requestSenderSettings = requestSenderSettings;
        }

        /// <summary>
        /// Transforms HttpResponseMessage into RequestResponseItem dto.
        /// </summary>
        public async Task<HttpResponseEnvelope> TransformAsync(
            Uri url,
            HttpResponseMessage? httpResponseMessage, 
            string userAccepts,
            string blobId,
            string? blobContainer,
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default)
        {
            if (httpResponseMessage == null) { 
                throw new ArgumentNullException(nameof(httpResponseMessage), "Http response object was null.");
            }

            byte[]? data = null;
            var contentType = ResolveContentType(httpResponseMessage.Content);
            var encoding = ResolveEncoding(httpResponseMessage.Content);
            var statusCode = httpResponseMessage.StatusCode;
            var lastModified = httpResponseMessage.Content?.Headers?.LastModified?.UtcDateTime ?? DateTimeOffset.UtcNow;
            var expires = httpResponseMessage.Content?.Headers?.Expires;
            var retryAfter = GetRetryAfterOffset(httpResponseMessage.StatusCode, httpResponseMessage?.Headers?.RetryAfter);
            var requestUrl = httpResponseMessage?.RequestMessage?.RequestUri;
            
            if (!IsContentAcceptable(contentType, userAccepts))
            {
                statusCode = HttpStatusCode.NotAcceptable;
            }

            if (statusCode == HttpStatusCode.OK)
            {
                data = await ReadAsByteArrayAsync(httpResponseMessage?.Content, contentMaxBytes, cancellationToken);
            }

            return new HttpResponseEnvelope
            {
                Metadata = new HttpResponseMetadata
                {
                    OriginalUrl = url,
                    Url = requestUrl, //after redirects if any
                    StatusCode = statusCode,
                    LastModified = lastModified,
                    Expires = expires,
                    RetryAfter = retryAfter,
                    ResponseData = new HttpResponseDataItem
                    {
                        BlobId = blobId,
                        BlobContainer = blobContainer,
                        ContentType = contentType,
                        Encoding = encoding
                    },
                },
                Data = new HttpResponseData
                {
                    Payload = data
                },
                IsFromCache = false
            };
        }

        /// <summary>
        /// Determines the media type of the given HTTP content from the header, 
        /// falling back to a default if necessary.
        /// </summary>
        private string ResolveContentType(HttpContent? content)
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

        public DateTimeOffset? GetRetryAfterOffset(
            HttpStatusCode statusCode, RetryConditionHeaderValue? retryAfter)
        {
            if (retryAfter == null)
            {
                // Use fallback delay when Retry-After header is missing
                if (statusCode is HttpStatusCode.TooManyRequests
                    or HttpStatusCode.Forbidden
                    or HttpStatusCode.ServiceUnavailable)
                {
                    return DateTimeOffset.UtcNow.Add(
                        TimeSpan.FromMinutes(_requestSenderSettings.RetryAfterFallbackMinutes));
                }
                return null;
            }

            if (retryAfter.Date.HasValue)
            {
                return retryAfter.Date.Value;
            }

            if (retryAfter.Delta.HasValue)
            {
                return DateTimeOffset.UtcNow + retryAfter.Delta.Value;
            }

            return null;
        }

        public static bool IsContentAcceptable(string? contentType, string userAccepts)
        {
            if (string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(userAccepts))
                return false;

            return userAccepts.Split(',')
                .Any(s => MediaTypeWithQualityHeaderValue
                    .TryParse(s, out var v) && (v.MediaType == "*/*" || v.MediaType == contentType || v.MediaType
                        .EndsWith("/*") && contentType
                            .StartsWith(v.MediaType
                                .Split('/')[0])));
        }

        public static async Task<byte[]?> ReadAsByteArrayAsync(
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

                if (bytesRead == 0) break;

                memoryStream.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }

            return memoryStream.ToArray();
        }

    }
}

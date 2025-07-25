using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Requests.Core
{
    public class RequestTransformer : IRequestTransformer
    {
        private const int DEFAULT_RETRY_AFTER_MINUTES = 1;

        /// <summary>
        /// Transforms HttpResponseMessage into RequestResponseItem dto.
        /// </summary>
        public async Task<RequestResponseItem> TransformAsync(
            HttpResponseMessage response, 
            string clientAccepts, 
            int contentMaxBytes = 0, 
            CancellationToken cancellationToken = default)
        {
            if (response == null) { 
                throw new ArgumentNullException(nameof(response), "Http response object was null.");
            }

            string? content = null;
            var statusCode = response.StatusCode;
            var contentType = response.Content?.Headers?.ContentType?.MediaType;
            var lastModified = response.Content?.Headers?.LastModified?.UtcDateTime ?? DateTimeOffset.UtcNow;
            var expires = response.Content?.Headers?.Expires;
            var retryAfter = GetRetryAfterOffset(response.StatusCode, response?.Headers?.RetryAfter);

            if (!IsContentAcceptable(contentType, clientAccepts))
                statusCode = HttpStatusCode.NotAcceptable;

            if (statusCode != HttpStatusCode.OK)
            {
                content = await ReadAsStringAsync(response.Content, contentMaxBytes, cancellationToken);
            }

            return new RequestResponseItem
            {
                Content = content,
                ContentType = contentType,
                StatusCode = statusCode,
                LastModified = lastModified,
                Expires = expires,
                RetryAfter = retryAfter
            };
        }

        public static DateTimeOffset? GetRetryAfterOffset(
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
                        TimeSpan.FromMinutes(DEFAULT_RETRY_AFTER_MINUTES));
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

        public static bool IsContentAcceptable(string? contentType, string clientAccepts)
        {
            if (string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(clientAccepts))
                return false;

            return clientAccepts.Split(',')
                .Any(s => MediaTypeWithQualityHeaderValue
                    .TryParse(s, out var v) && (v.MediaType == "*/*" || v.MediaType == contentType || v.MediaType
                        .EndsWith("/*") && contentType
                            .StartsWith(v.MediaType
                                .Split('/')[0])));
        }

        public static async Task<string?> ReadAsStringAsync(
            HttpContent? content,
            int contentMaxBytes,
            CancellationToken cancellationToken = default)
        {
            if (content == null)
                return null;

            if (contentMaxBytes <= 0)
                return await content.ReadAsStringAsync(cancellationToken);

            using var stream = await content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var buffer = new char[1024];
            int totalBytes = 0;
            var builder = new StringBuilder();

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                var chunk = new string(buffer, 0, charsRead);
                int chunkByteCount = Encoding.UTF8.GetByteCount(chunk);

                if (totalBytes + chunkByteCount > contentMaxBytes)
                {
                    int allowedBytes = contentMaxBytes - totalBytes;

                    // Trim the chunk to fit the remaining byte allowance
                    int allowedChars = chunk.Length;
                    while (allowedChars > 0 && Encoding.UTF8.GetByteCount(chunk[..allowedChars]) > allowedBytes)
                    {
                        allowedChars--;
                    }

                    builder.Append(chunk[..allowedChars]);
                    break;
                }

                builder.Append(chunk);
                totalBytes += chunkByteCount;
            }

            return builder.ToString();
        }

    }
}

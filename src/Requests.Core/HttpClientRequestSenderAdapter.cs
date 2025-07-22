using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using Logging.Core;

namespace Requests.Core
{
    public class HttpClientRequestSenderAdapter : IRequestSender
    {
        private readonly IAppLogger _logger;
        private readonly HttpClient _httpClient;

        private const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        private const string DEFAULT_CLIENT_ACCEPT = "text/html";
        private const int DEFAULT_REQUEST_SIZE_LIMIT = 1_048_576; //1Mb
        private const int MAX_RETRY_ATTEMPTS = 3;

        public HttpClientRequestSenderAdapter(IAppLogger logger)
        {
            _logger = logger;   
            _httpClient = new HttpClient();
        }

        public async Task<RequestResponse?> GetStringAsync(Uri url, CancellationToken cancellationToken = default)
        {
            return await GetStringAsync(url, string.Empty, string.Empty, 0, cancellationToken);
        }

        public async Task<RequestResponse?> GetStringAsync(Uri url, string userAgent, string clientAccepts, int attempt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = DEFAULT_USER_AGENT;

            if (string.IsNullOrWhiteSpace(clientAccepts))
                clientAccepts = DEFAULT_CLIENT_ACCEPT;

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd(clientAccepts);

            try
            {
                var response = await _httpClient.GetAsync(url.AbsoluteUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                var statusCode = response.StatusCode;
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var lastModified = response.Content?.Headers?.LastModified?.UtcDateTime ?? DateTimeOffset.UtcNow;
                var expires = response.Content?.Headers?.Expires?.UtcDateTime;
                var retryAfter = GetRetryAfterDate(response);
                var retryAttempt = attempt;
                string? content = null;

                if (!IsContentAcceptable(contentType,clientAccepts))
                    statusCode = HttpStatusCode.NotAcceptable;

                _logger.LogInformation($"GET: {url.AbsoluteUri} Status Code: {response.StatusCode} Attempt: {attempt}");

                if (!SetRetryAfterThrottle(statusCode, attempt, MAX_RETRY_ATTEMPTS, ref retryAttempt, ref retryAfter))
                {
                    _logger.LogInformation($"DISCARDED: {url.AbsoluteUri} Status Code: {response.StatusCode} Attempt: {attempt}");
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    content = await ReadAsStringWithSizeLimitAsync(response, DEFAULT_REQUEST_SIZE_LIMIT, cancellationToken);
                }

                return new RequestResponse
                {
                    Content = content,
                    ContentType = contentType,
                    StatusCode = response.StatusCode,
                    LastModified = lastModified,
                    Expires = expires,
                    RetryAfter = retryAfter,
                    RetryAttempt = retryAttempt
                };
            }
            catch (Exception ex)
            {
                //CATCH ALL:
                //OperationCanceledException: cancellation token is triggered
                //HttpRequestException: Connection issues, DNS failures, SSL errors
                //TaskCanceledException: Timeouts
                //OperationCanceledException: Explicit cancellation via CancellationToken
                //InvalidOperationException: Misconfigured HttpClient
                _logger.LogError($"GET: {url.AbsoluteUri} Exception: {ex.Message} InnerException: {ex.InnerException}");
            }

            return null;
        }

        private static bool IsContentAcceptable(string? contentType, string clientAccepts)
        {
            if (string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(clientAccepts)) 
                return false;

            var acceptsTypes = clientAccepts.Split(',').Select(t => t.Trim().ToLowerInvariant());

            return (!acceptsTypes.Contains(contentType));
        }

        private static bool SetRetryAfterThrottle(HttpStatusCode statusCode, int attempt, int maxAttempts, ref int retryAttempt, ref DateTimeOffset? retryAfter)
        {
            if (statusCode != HttpStatusCode.TooManyRequests || attempt >= maxAttempts)
                return false;

            retryAttempt++;

            if (retryAfter == null)
                retryAfter = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(Math.Pow(2, retryAttempt)));

            return true;
        }

        private static DateTimeOffset? GetRetryAfterDate(HttpResponseMessage response)
        {
            var retryAfterHeader = response?.Headers?.RetryAfter;
            if (retryAfterHeader == null)
                return null;

            if (retryAfterHeader.Date.HasValue)
            {
                return retryAfterHeader.Date.Value;
            }

            if (retryAfterHeader.Delta.HasValue)
            {
                return DateTimeOffset.UtcNow + retryAfterHeader.Delta.Value;
            }

            return null;
        }

        private static async Task<string> ReadAsStringWithSizeLimitAsync(HttpResponseMessage response, int maxBytes, CancellationToken cancellationToken = default)
        {
            if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
                return string.Empty;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            var builder = new StringBuilder();
            int totalBytes = 0;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                if (line == null) break;

                var lineBytes = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                totalBytes += lineBytes;

                if (totalBytes > maxBytes)
                {
                    break;
                }   

                builder.AppendLine(line);
            }

            return builder.ToString();
        }

    }
}

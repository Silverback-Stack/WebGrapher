using System;

namespace Requests.Core
{
    internal class HttpClientAdapter : IHttpRequester
    {
        private readonly HttpClient _client;

        public HttpClientAdapter(HttpClient httpClient)
        {
            _client = httpClient;
        }

        public async Task<HttpResponseMessage?> GetAsync(
            Uri uri,
            string userAgent,
            string acceptHeader,
            CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(userAgent);
            request.Headers.Accept.ParseAdd(acceptHeader);

            return await _client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }

}

using System;

namespace Requests.Core
{
    public class HttpClientAdapter : IHttpRequester
    {
        private readonly HttpClient _client;

        public HttpClientAdapter(HttpClient httpClient)
        {
            _client = httpClient;
        }

        public async Task<HttpResponseMessage?> GetAsync(
            Uri uri,
            string userAgent,
            string userAccepts,
            CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(userAgent);
            request.Headers.Accept.ParseAdd(userAccepts);

            return await _client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }

}

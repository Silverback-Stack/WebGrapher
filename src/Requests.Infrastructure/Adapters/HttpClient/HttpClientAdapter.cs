using Requests.Core;
using System;

namespace Requests.Infrastructure.Adapters.HttpClient
{
    public class HttpClientAdapter : IHttpRequester
    {
        private readonly System.Net.Http.HttpClient _client;

        public HttpClientAdapter(System.Net.Http.HttpClient httpClient)
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

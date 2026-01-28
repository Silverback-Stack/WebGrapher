using System;
using Caching.Core;
using Microsoft.Extensions.Logging;
using Requests.Core;
using System.Net;
using Requests.Infrastructure.Adapters.HttpClient;

namespace Requests.Factories
{
    public static class RequestsFactory
    {
        public static IRequestSender Create(ILogger logger, ICache metaCache, ICache blobCache, RequestsConfig requestsConfig)
        {
            IHttpRequester httpRequester;

            switch (requestsConfig.Provider)
            {
                case TransportProvider.HttpClient:
                    httpRequester = BuildHttpClientRequester(requestsConfig.HttpClient);
                    break;

                default:
                    throw new NotSupportedException($"Transport '{requestsConfig.Provider}' is not supported.");
            }

            IRequestTransformer requestTransformer = new RequestTransformer(requestsConfig.RequestSender);

            return new RequestSender(
                logger, 
                metaCache, 
                blobCache, 
                httpRequester, 
                requestTransformer,
                requestsConfig.RequestSender);
        }

        private static IHttpRequester BuildHttpClientRequester(HttpClientSettings httpClientSettings)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = httpClientSettings.AllowAutoRedirect,
                MaxAutomaticRedirections = httpClientSettings.MaxAutomaticRedirections,
                AutomaticDecompression = DecompressionMethods.All
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(httpClientSettings.TimeoutSeconds)
            };

            return new HttpClientAdapter(client);
        }
    }
}

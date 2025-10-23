using System;
using Caching.Core;
using Microsoft.Extensions.Logging;
using Requests.Core.Adapters;

namespace Requests.Core
{
    public static class RequestFactory
    {
        public static IRequestSender Create(ILogger logger, ICache metaCache, ICache blobCache, RequestSenderSettings requestSenderSettings)
        {
            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = requestSenderSettings.AllowAutoRedirect,
                MaxAutomaticRedirections = requestSenderSettings.MaxAutomaticRedirections,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            IHttpRequester httpRequester = new HttpClientAdapter(new HttpClient(httpClientHandler) { 
                Timeout = TimeSpan.FromSeconds(requestSenderSettings.TimoutSeconds) 
            });

            IRequestTransformer requestTransformer = new RequestTransformer(requestSenderSettings);

            return new RequestSender(logger, metaCache, blobCache, httpRequester, requestTransformer, requestSenderSettings);
        }
    }
}

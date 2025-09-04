using System;
using Caching.Core;
using Microsoft.Extensions.Logging;
using Requests.Core.Adapters;

namespace Requests.Core
{
    public static class RequestFactory
    {
        public static IRequestSender CreateRequestSender(ILogger logger, ICache metaCache, ICache blobCache, RequestSenderSettings requestSenderSettings)
        {
            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };

            IHttpRequester httpRequester = new HttpClientAdapter(new HttpClient(httpClientHandler));

            IRequestTransformer requestTransformer = new RequestTransformer(requestSenderSettings);

            return new RequestSender(logger, metaCache, blobCache, httpRequester, requestTransformer, requestSenderSettings);
        }
    }
}

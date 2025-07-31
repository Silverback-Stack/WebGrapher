using System;
using Caching.Core;
using Microsoft.Extensions.Logging;
using Requests.Core.Adapters;

namespace Requests.Core
{
    public static class RequestFactory
    {
        public static IRequestSender CreateRequestSender(ILogger logger, ICache cache)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            IHttpRequester httpRequester = new HttpClientAdapter(new HttpClient(handler));

            IRequestTransformer requestTransformer = new RequestTransformer();

            return new RequestSender(logger, cache, httpRequester, requestTransformer);
        }
    }
}

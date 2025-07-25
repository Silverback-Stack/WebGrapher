using System;
using Caching.Core;
using Logging.Core;

namespace Requests.Core
{
    public static class RequestFactory
    {
        public static IRequestSender CreateRequestSender(ILogger logger, ICache cache)
        {
            IHttpRequester httpRequester = new HttpClientAdapter(new HttpClient());
            IRequestTransformer requestTransformer = new RequestTransformer();

            return new RequestSender(logger, cache, httpRequester, requestTransformer);
        }
    }
}

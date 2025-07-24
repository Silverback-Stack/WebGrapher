using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Logging.Core;

namespace Requests.Core
{
    public static class RequestFactory
    {
        public static IRequestSender CreateRequestSender(ILogger logger, ICache cache)
        {
            return new HttpClientRequestSenderAdapter(logger, cache);
        }
    }
}

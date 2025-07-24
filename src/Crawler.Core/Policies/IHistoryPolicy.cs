using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.EventTypes;

namespace Crawler.Core.Policies
{
    public interface IHistoryPolicy
    {
        void SetResponseStatus(Uri url, HttpStatusCode statusCode, DateTimeOffset? retryAfter);
    }
}

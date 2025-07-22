using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core.RobotsEvaluator
{
    public static class RobotsFactory
    {
        public static IRobotsEvaluator CreateRobotsEvaluator(
            IAppLogger logger, 
            ICache cache, 
            IRequestSender requestSender)
        {
            return new RobotsEvaluator(logger, cache, requestSender);
        }
    }
}

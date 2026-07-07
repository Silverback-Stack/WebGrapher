using Caching.Core;
using Microsoft.Extensions.Logging;
using Requests.Core;
using SitePolicy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitePolicyFactories
{
    public static class SitePolicyFactory
    {
        public static ISitePolicyResolver Create(
            ILogger logger,
            ICache policyCache,
            IRequestSender requestSender,
            SitePolicyConfig sitePolicyConfig)
        {
            if (sitePolicyConfig is null)
                throw new ArgumentNullException(nameof(sitePolicyConfig));

            return new SitePolicyResolver(
                logger, 
                policyCache, 
                requestSender,
                sitePolicyConfig.Settings);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core.Policies
{
    public interface IRateLimitPolicy
    {
        bool IsRateLimited(Uri url);
    }
}

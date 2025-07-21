using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core.Robots
{
    public interface IRobotsService
    {
        Task<bool> IsUrlAllowedAsync(Uri url, string userAgent = "*", CancellationToken cancellationToken = default);
    }
}

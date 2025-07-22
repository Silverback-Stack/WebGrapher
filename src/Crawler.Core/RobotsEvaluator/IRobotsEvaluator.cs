using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core.RobotsEvaluator
{
    public interface IRobotsEvaluator
    {
        Task<bool> IsUrlPermittedAsync(Uri url, string? userAgent, CancellationToken cancellationToken = default);
    }
}

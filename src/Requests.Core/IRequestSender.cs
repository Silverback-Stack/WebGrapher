using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Requests.Core
{
    public interface IRequestSender
    {
        Task<RequestResponse?> GetStringAsync(Uri url, CancellationToken cancellationToken = default);
        Task<RequestResponse?> GetStringAsync(Uri url, string? userAgent, string? clientAccepts, int attempt, CancellationToken cancellationToken = default);
    }
}

using System;
using Requests.Core;

namespace Scraper.Core
{
    public interface IPageScraper
    {
        Task StartAsync();
        Task StopAsync();
        Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent,
            string clientAccept,
            string compositeKey = "",
            CancellationToken cancellationToken = default);
    }
}

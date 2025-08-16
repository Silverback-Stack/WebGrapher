using System;
using Requests.Core;

namespace Scraper.Core
{
    public interface IPageScraper
    {
        Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent,
            string clientAccept,
            string compositeKey = null,
            CancellationToken cancellationToken = default);
    }
}

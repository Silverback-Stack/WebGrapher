using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Scraper.Core
{
    public class PageScraper : BaseScraper
    {
        private const int DEFAULT_CONTENT_MAX_BYTES = 1_048_576; //1Mb

        public PageScraper(ILogger logger, IEventBus eventBus, IRequestSender requestSender) 
            : base(logger, eventBus, requestSender) { }

        public async override Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? clientAccept)
        {
            var httpResponseEnvelope = await _requestSender.GetStringAsync(
                url,
                userAgent,
                clientAccept,
                DEFAULT_CONTENT_MAX_BYTES);

            if (response?.Data != null) {
                return new ScrapeResponseItem()
                {
                    RequestUrl = response.Data.OriginalUrl,
                    ResolvedUrl = response.Data.Url,
                    Content = response.Data.TextContent,
                    StatusCode = response.Data.StatusCode,
                    LastModified = response.Data.LastModified,
                    RetryAfter = response.Data.RetryAfter,
                    IsFromCache = response.IsFromCache
                };
            }

            return null;
        }
    }
}

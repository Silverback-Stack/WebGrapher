using System;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;
using ScraperService;

namespace Scraper.Core
{
    public class PageScraper : BaseScraper
    {
        private const int DEFAULT_CONTENT_MAX_BYTES = 1_048_576; //1Mb

        public PageScraper(ILogger appLogger, IEventBus eventBus, IRequestSender requestSender) 
            : base(appLogger, eventBus, requestSender) { }

        public async override Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? clientAccept)
        {
            var response = await _requestSender.GetStringAsync(
                url,
                userAgent,
                clientAccept,
                DEFAULT_CONTENT_MAX_BYTES);

            if (response != null) { 
                return new ScrapeResponseItem()
                {
                    Content = response.Content ?? string.Empty,
                    StatusCode = response.StatusCode,
                    LastModified = response.LastModified,
                    RetryAfter = response.RetryAfter
                };
            }

            return null;
        }
    }
}

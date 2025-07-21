using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.Types;
using Logging.Core;

namespace ScraperService
{
    public abstract class BaseScraper : IScraper
    {
        internal readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public BaseScraper(IEventBus eventBus)
        {
            _logger = LoggingFactory.Create(LoggingOptions.File, nameof(BaseScraper));
            _eventBus = eventBus;
            RegisterEvents();
        }

        public async Task StartAsync()
        {
            await _eventBus.StartAsync();
            _eventBus.Subscribe<ScrapePageEvent>(async evt =>
            {
                await HandleEvent(evt);
                await Task.CompletedTask;
            });
        }

        public async Task StopAsync()
        {
            await _eventBus.StopAsync();
        }

        private void RegisterEvents()
        {
            
        }

        private async Task HandleEvent(ScrapePageEvent evt)
        {
            var responseDto = await GetAsync(
                evt.CrawlPageEvent.Url, 
                evt.CrawlPageEvent.UserAgent, 
                evt.CrawlPageEvent.ClientAccepts);

            if (responseDto != null && responseDto.StatusCode == HttpStatusCode.OK)
            {
                await _eventBus.PublishAsync(new ParsePageEvent
                {
                    CrawlPageEvent = evt.CrawlPageEvent,
                    CreatedAt = DateTimeOffset.UtcNow,
                    HtmlContent = responseDto.HtmlContent,
                    LastModified = responseDto.LastModified,
                    StatusCode = responseDto.StatusCode
                });
            }
        }

        public abstract Task<ResponseDto> GetAsync(Uri url, string userAgent, string clientAccept);

        public void Dispose()
        {
            _eventBus?.Dispose();
        }
    }

}

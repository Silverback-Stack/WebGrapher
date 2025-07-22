using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.Types;
using Logging.Core;

namespace ParserService
{
    public abstract class BasePageParser : IPageParser
    {
        private readonly IAppLogger _appLogger;
        private readonly IEventBus _eventBus;

        public BasePageParser(IAppLogger appLogger, IEventBus eventBus)
        {
            _appLogger = appLogger;
            _eventBus = eventBus;
        }

        public async Task StartAsync()
        {
            await _eventBus.StartAsync();

            _eventBus.Subscribe<ParsePageEvent>(async evt =>
            {
                await HandleEvent(evt);
                await Task.CompletedTask;
            });
        }

        public async Task StopAsync()
        {
            await _eventBus.StopAsync();
        }

        public void Dispose()
        {
            _eventBus.Dispose();
        }

        private async Task HandleEvent(ParsePageEvent evt)
        {
            var pageDto = Parse(evt.HtmlContent);

            if (pageDto != null)
            {
                await _eventBus.PublishAsync(new NormalisePageEvent
                {
                    CrawlPageEvent = evt.CrawlPageEvent,
                    Title = pageDto.Title,
                    Keywords = pageDto.Content,
                    Links = pageDto.Links,
                    CreatedAt = DateTimeOffset.UtcNow,
                    StatusCode = evt.StatusCode,
                    LastModified = evt.LastModified
                });
            }
        }

        public abstract Page Parse(string content);
    }
}

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
    public abstract class BasePageParser : IPageParser, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public BasePageParser(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void Start()
        {
            Subscribe();
        }

        public void Subscribe()
        {
            _eventBus.Subscribe<ParsePageEvent>(EventHandler);
        }

        public void Unsubscribe()
        {
            _eventBus.Unsubscribe<ParsePageEvent>(EventHandler);
        }

        private async Task EventHandler(ParsePageEvent evt)
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
            await Task.CompletedTask;
        }

        public abstract Page Parse(string content);
    }
}

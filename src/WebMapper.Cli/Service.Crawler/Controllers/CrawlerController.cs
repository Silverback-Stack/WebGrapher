using Events.Core.Bus;
using Events.Core.Events;
using System;
using Microsoft.AspNetCore.Mvc;

namespace WebMapper.Cli.Service.Crawler.Controllers
{
    [ApiController]
    [Route("api")]
    public class CrawlerController : ControllerBase
    {
        private readonly IEventBus _eventBus;

        public CrawlerController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        [HttpPost("crawl")]
        public async Task<IActionResult> CrawlUrlAsync(CrawlPageDto crawlPageDto)
        {
            var crawlPageEvent = new CrawlPageEvent(
            crawlPageDto.Url,
            crawlPageDto.GraphId,
            crawlPageDto.FollowExternalLinks,
            crawlPageDto.ExcludeQueryStrings,
            crawlPageDto.MaxDepth,
            crawlPageDto.MaxLinks,
            crawlPageDto.UrlMatchRegex,
            crawlPageDto.TitleElementXPath,
            crawlPageDto.ContentElementXPath,
            crawlPageDto.SummaryElementXPath,
            crawlPageDto.ImageElementXPath,
            crawlPageDto.RelatedLinksElementXPath,
            crawlPageDto.UserAgent,
            crawlPageDto.UserAccepts
            );

            await _eventBus.PublishAsync(crawlPageEvent);

            return Ok(new { message = $"Crawl event published for {crawlPageDto.Url}" });
        }
    }
}

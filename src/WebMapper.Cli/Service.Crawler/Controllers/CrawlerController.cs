using Events.Core.Bus;
using Events.Core.EventTypes;
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
            url: crawlPageDto.Url,
            mapId: crawlPageDto.MapId,
            correlationId: Guid.NewGuid(),
            followExternalLinks: crawlPageDto.FollowExternalLinks,
            removeQueryStrings: crawlPageDto.RemoveQueryStrings,
            maxDepth: crawlPageDto.MaxDepth,
            pathFilters: crawlPageDto.PathFilters?.Split(',').ToArray(),
            userAgent: crawlPageDto.UserAgent,
            userAccepts: crawlPageDto.UserAccepts
            );

            await _eventBus.PublishAsync(crawlPageEvent);

            return Ok(new { message = $"Crawl event published for {crawlPageDto.Url}" });
        }
    }
}

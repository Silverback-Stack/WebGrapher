using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record CrawlPageEvent
    {
        public CrawlPageRequestDto CrawlPageRequest { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        private CrawlPageEvent() { }

        private CrawlPageEvent(
            Uri url,
            int graphId,
            Guid correlationId,
            int attempt,
            int depth,
            int maxDepth,
            bool followExternalLinks,
            bool excludeQueryStrings,
            string titleFilterXPath,
            string contentFilterXPath,
            string relatedContentFilterXPath,
            string linkUrlFilterXPath,
            string imageUrlFilterXPath,
            string userAgent,
            string userAccepts) 
        {
            CrawlPageRequest = new CrawlPageRequestDto
            {
                Url = url,
                GraphId = graphId,
                CorrelationId = correlationId,
                Attempt = 1,
                Depth = 0,
                MaxDepth = maxDepth,
                FollowExternalLinks = followExternalLinks,
                ExcludeQueryStrings = excludeQueryStrings,
                TitleFilterXPath = titleFilterXPath,
                ContentFilterXPath = contentFilterXPath,
                RelatedContentFilterXPath = relatedContentFilterXPath,
                LinkUrlFilterXPath = linkUrlFilterXPath,
                ImageUrlFilterXPath = imageUrlFilterXPath,
                UserAgent = userAgent,
                UserAccepts = userAccepts,
                RequestedAt = DateTimeOffset.UtcNow
            };
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public CrawlPageEvent(
            Uri url,
            int graphId,
            bool followExternalLinks,
            bool excludeQueryStrings,
            int maxDepth,
            string titleFilterXPath,
            string contentFilterXPath,
            string relatedContentFilterXPath,
            string linkUrlFilterXPath,
            string imageUrlFilterXPath,
            string userAgent,
            string userAccepts)
            : this(
                url,
                graphId,
                correlationId: Guid.NewGuid(),
                attempt: 1,
                depth: 0,
                maxDepth,
                followExternalLinks,
                excludeQueryStrings,
                titleFilterXPath,
                contentFilterXPath,
                relatedContentFilterXPath,
                linkUrlFilterXPath,
                imageUrlFilterXPath,
                userAgent,
                userAccepts) { }


        public CrawlPageEvent(
            Uri url,
            int attempt,
            int depth,
            CrawlPageRequestDto crawlPageRequest)
            : this(
                url,
                crawlPageRequest.GraphId,
                crawlPageRequest.CorrelationId,
                attempt,
                depth,
                crawlPageRequest.MaxDepth,
                crawlPageRequest.FollowExternalLinks,
                crawlPageRequest.ExcludeQueryStrings,
                crawlPageRequest.TitleFilterXPath,
                crawlPageRequest.ContentFilterXPath,
                crawlPageRequest.RelatedContentFilterXPath,
                crawlPageRequest.LinkUrlFilterXPath,
                crawlPageRequest.ImageUrlFilterXPath,
                crawlPageRequest.UserAgent,
                crawlPageRequest.UserAccepts) { } 

    }
}

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
            Guid graphId,
            Guid correlationId,
            int attempt,
            int depth,
            int maxDepth,
            int maxLinks,
            bool followExternalLinks,
            bool excludeQueryStrings,
            string urlMatchRegex,
            string titleElementXPath,
            string contentElementXPath,
            string summaryElementXPath,
            string imageElementXPath,
            string relatedLinksElementXPath,
            string userAgent,
            string userAccepts) 
        {
            CrawlPageRequest = new CrawlPageRequestDto
            {
                Url = url,
                GraphId = graphId,
                CorrelationId = correlationId,
                Attempt = attempt,
                Depth = depth,
                MaxDepth = maxDepth,
                MaxLinks = maxLinks,
                FollowExternalLinks = followExternalLinks,
                ExcludeQueryStrings = excludeQueryStrings,
                UrlMatchRegex = urlMatchRegex,
                TitleElementXPath = titleElementXPath,
                ContentElementXPath = contentElementXPath,
                SummaryElementXPath = summaryElementXPath,
                ImageElementXPath = imageElementXPath,
                RelatedLinksElementXPath = relatedLinksElementXPath,         
                UserAgent = userAgent,
                UserAccepts = userAccepts,
                RequestedAt = DateTimeOffset.UtcNow
            };
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public CrawlPageEvent(
            Uri url,
            Guid graphId,
            bool followExternalLinks,
            bool excludeQueryStrings,
            int maxDepth,
            int maxLinks,
            string urlMatchRegex,
            string titleElementXPath,
            string contentElementXPath,
            string summaryElementXPath,
            string imageElementXPath,
            string relatedLinksElementXPath,
            string userAgent,
            string userAccepts)
            : this(
                url,
                graphId,
                correlationId: Guid.NewGuid(),
                attempt: 1,
                depth: 0,
                maxDepth,
                maxLinks,
                followExternalLinks,
                excludeQueryStrings,
                urlMatchRegex,
                titleElementXPath,
                contentElementXPath,
                summaryElementXPath,
                imageElementXPath,
                relatedLinksElementXPath,
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
                crawlPageRequest.MaxLinks,
                crawlPageRequest.FollowExternalLinks,
                crawlPageRequest.ExcludeQueryStrings,
                crawlPageRequest.UrlMatchRegex,
                crawlPageRequest.TitleElementXPath,
                crawlPageRequest.ContentElementXPath,
                crawlPageRequest.SummaryElementXPath,
                crawlPageRequest.ImageElementXPath,
                crawlPageRequest.RelatedLinksElementXPath,
                crawlPageRequest.UserAgent,
                crawlPageRequest.UserAccepts) { } 

    }
}

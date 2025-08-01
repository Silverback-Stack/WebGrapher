using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Microsoft.Extensions.Logging;

namespace Normalisation.Core
{
    public class HtmlNormalisation : IHtmlNormalisation, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        private const int MAX_TITLE_LENGTH = 200;
        private const int MAX_KEYWORD_LENGTH = 4000;
        private const int MAX_LINKS_PER_PAGE = 1000;
        private static readonly string[] ALLOWABLE_LINK_SCHEMAS = ["http", "https"];

        public HtmlNormalisation(ILogger logger, IEventBus eventBus)
        {
            _logger = logger; 
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<NormalisePageEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<NormalisePageEvent>(EventHandler);
        }

        private async Task EventHandler(NormalisePageEvent evt)
        {
            var normalisedTitle = NormaliseTitle(evt.Title);
            var normalisedKeywords = NormaliseKeywords(evt.Keywords);
            var normalisedLinks = NormaliseLinks(
                evt.Links, 
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.FollowExternalLinks,
                evt.CrawlPageEvent.RemoveQueryStrings,
                evt.CrawlPageEvent.PathFilters);

            await _eventBus.PublishAsync(new GraphPageEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                RequestUrl = evt.RequestUrl,
                ResolvedUrl = evt.ResolvedUrl,
                Title = normalisedTitle,
                Keywords = normalisedKeywords,
                Links = normalisedLinks,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = evt.StatusCode,
                SourceLastModified = evt.LastModified
            });

            var linkType = evt.CrawlPageEvent.FollowExternalLinks ? "external" : "internal";
            _logger.LogDebug($"Normalised Page: {evt.ResolvedUrl} found {normalisedLinks.Count()} {linkType} links and extracted {normalisedKeywords.Count()} keywords.");
        }

        public string NormaliseTitle(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            text = TextNormaliser.Truncate(text, MAX_TITLE_LENGTH);

            return text;
        }

        public string NormaliseContent(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.CollapseWhitespace(text);
            text = TextNormaliser.Truncate(text, MAX_KEYWORD_LENGTH);

            return text;
        }

        public string NormaliseKeywords(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.ToLowerCase(text);
            text = TextNormaliser.RemovePunctuation(text);
            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            text = StopWordFilter.RemoveStopWords(text, 
                LanguageIdentifier.DetectLanguage(text));
            text = TextNormaliser.RemoveDuplicateWords(text);
            text = TextNormaliser.Truncate(text, MAX_KEYWORD_LENGTH);

            return text;
        }

        public IEnumerable<Uri> NormaliseLinks(
            IEnumerable<string> links, 
            Uri baseUrl, 
            bool allowExternal, 
            bool removeQueryStrings, 
            IEnumerable<string>? pathFilters)
        {
            var uniqueUrls = UrlNormaliser.MakeAbsolute(links, baseUrl);

            uniqueUrls = UrlNormaliser.RemoveCyclicalLinks(uniqueUrls, baseUrl);

            uniqueUrls = UrlNormaliser.RemoveTrailingSlash(uniqueUrls);

            uniqueUrls = UrlNormaliser.FilterBySchema(uniqueUrls, ALLOWABLE_LINK_SCHEMAS);

            if (!allowExternal)
                uniqueUrls = UrlNormaliser.RemoveExternalLinks(uniqueUrls, baseUrl);

            if (removeQueryStrings)
                uniqueUrls = UrlNormaliser.RemoveQueryStrings(uniqueUrls);

            uniqueUrls = UrlNormaliser.FilterByPath(uniqueUrls, pathFilters);

            uniqueUrls = UrlNormaliser.Truncate(uniqueUrls, MAX_LINKS_PER_PAGE);

            return uniqueUrls;
        }
    }
}

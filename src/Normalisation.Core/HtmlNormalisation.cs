using System;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Logging.Core;

namespace Normalisation.Core
{
    public class HtmlNormalisation : IHtmlNormalisation, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        private const int MAX_TITLE_LENGTH = 200;
        private const int MAX_KEYWORD_LENGTH = 4000;
        private const int MAX_LINKS_PER_PAGE = 1000;
        private static readonly string[] ALLOWABLE_SCHEMAS = ["http", "https"];

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
            var normalisedTitle = NormaliseKeywords(evt.Title);
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
                Title = normalisedTitle,
                Keywords = normalisedKeywords,
                Links = normalisedLinks,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = evt.StatusCode,
                SourceLastModified = evt.LastModified
            });
        }

        public string NormaliseTitle(string text)
        {
            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            text = TextNormaliser.Truncate(text, MAX_TITLE_LENGTH);

            return text;
        }

        public string NormaliseContent(string text)
        {
            text = TextNormaliser.CollapseWhitespace(text);
            text = TextNormaliser.Truncate(text, MAX_KEYWORD_LENGTH);

            return text;
        }

        public string NormaliseKeywords(string text)
        {
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

        public IEnumerable<string> NormaliseLinks(
            IEnumerable<string> links, 
            Uri baseUrl, 
            bool allowExternal, 
            bool removeQueryStrings, 
            IEnumerable<string> pathFilters)
        {
            links = UrlNormaliser.MakeAbsolute(links, baseUrl);
            links = UrlNormaliser.FilterBySchema(links, ALLOWABLE_SCHEMAS);
            links = allowExternal ? links : UrlNormaliser.RemoveExternalLinks(links, baseUrl);
            links = removeQueryStrings ? UrlNormaliser.RemoveQueryStrings(links) : links;
            links = UrlNormaliser.FilterByPath(links, pathFilters);
            links = UrlNormaliser.Truncate(links, MAX_LINKS_PER_PAGE);

            return links;
        }
    }
}

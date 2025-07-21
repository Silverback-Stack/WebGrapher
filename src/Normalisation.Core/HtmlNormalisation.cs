using System;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using Events.Core.Bus;
using Events.Core.Types;

namespace Normalisation.Core
{
    public class HtmlNormalisation : IHtmlNormalisation
    {
        private readonly IEventBus _eventBus;

        private const int MAX_TITLE_LENGTH = 200;
        private const int MAX_KEYWORD_LENGTH = 4000;
        private const int MAX_LINKS_PER_PAGE = 1000;
        private static readonly string[] ALLOWABLE_SCHEMAS = ["http", "https"];

        public HtmlNormalisation(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task StartAsync()
        {
            await _eventBus.StartAsync();

            _eventBus.Subscribe<NormalisePageEvent>(async evt =>
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

        private async Task HandleEvent(NormalisePageEvent evt)
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

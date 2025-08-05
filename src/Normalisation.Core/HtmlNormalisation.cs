using System;
using System.Text;
using Caching.Core;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Microsoft.Extensions.Logging;
using Normalisation.Core.Processors;

namespace Normalisation.Core
{
    public class HtmlNormalisation : IHtmlNormalisation, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly ICache _blobCache;
        private readonly IEventBus _eventBus;

        private const int MAX_TITLE_LENGTH = 60;
        private const int MAX_KEYWORD_LENGTH = 4069;
        private const int MAX_LINKS_PER_PAGE = 10;

        private static readonly string[] ALLOWABLE_LINK_SCHEMAS = ["http", "https"];

        public HtmlNormalisation(ILogger logger, ICache blobCache, IEventBus eventBus)
        {
            _logger = logger;
            _blobCache = blobCache;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<NormalisePageEvent>(NormalisePageContentAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<NormalisePageEvent>(NormalisePageContentAsync);
        }

        private async Task PublishGraphEvent(NormalisePageEvent evt,
            string? title, string? keywords, IEnumerable<Uri>? links)
        {
            await _eventBus.PublishAsync(new GraphPageEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                OriginalUrl = evt.OriginalUrl,
                IsRedirect = evt.IsRedirect,
                Url = evt.Url,
                Title = title,
                Keywords = keywords,
                Links = links,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = evt.StatusCode,
                SourceLastModified = evt.LastModified
            }, priority: evt.CrawlPageEvent.Depth);
        }

        public async Task NormalisePageContentAsync(NormalisePageEvent evt)
        {
            if (evt.BlobId is null || evt.Encoding is null)
            {
                _logger.LogWarning("yikes! where did the data go?");
                return;
            }

            var htmlDocument = await GetHtmlDocumentAsync(evt.BlobId, evt.BlobContainer, evt.Encoding);
            if (htmlDocument is null)
            {
                _logger.LogWarning("yikes! failed to read html document");
                return;
            }

            var htmlParser = new HtmlParser(htmlDocument);
            var extractedTitle = htmlParser.ExtractTitle();
            var extractedContent = htmlParser.ExtractContentAsPlainText();
            var extractedLinks = htmlParser.ExtractLinks();

            var normalisedTitle = NormaliseTitle(extractedTitle);
            var normalisedKeywords = NormaliseKeywords(extractedContent);
            var normalisedLinks = NormaliseLinks(
                extractedLinks,
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.FollowExternalLinks,
                evt.CrawlPageEvent.RemoveQueryStrings,
                evt.CrawlPageEvent.PathFilters);

            await PublishGraphEvent(evt, normalisedTitle, normalisedKeywords, normalisedLinks);

            var linkType = evt.CrawlPageEvent.FollowExternalLinks ? "external" : "internal";
            _logger.LogDebug($"Publishing GraphPageEvent for {evt.Url} with {normalisedLinks.Count()} {linkType} links and {normalisedKeywords.Count()} keywords.");
        }

        private async Task<string?> GetHtmlDocumentAsync(string blobId, string? container, string encoding)
        {
            var cacheKey = container is not null ? Path.Combine(container, blobId) : blobId;

            var blob = await _blobCache.GetAsync<byte[]>(cacheKey);
            if (blob is null)
            {
                _logger.LogWarning($"Blob {blobId} was not found in the cache.");
                return null;
            }

            try
            {
                var encoder = Encoding.GetEncoding(encoding);
                return encoder.GetString(blob);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to decode cached blob {blobId}");
                return null;
            }
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
            text = StopWordFilter.RemoveStopWords(text, LanguageIdentifier.DetectLanguage(text));
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

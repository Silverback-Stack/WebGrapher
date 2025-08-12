using System;
using System.Text;
using Caching.Core;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Microsoft.Extensions.Logging;
using Normalisation.Core.Processors;

namespace Normalisation.Core
{
    public class PageNormaliser : IPageNormaliser, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly ICache _blobCache;
        private readonly IEventBus _eventBus;

        private const int MAX_TITLE_LENGTH = 100;
        private const int MAX_KEYWORDS = 300; //1 page of text
        private const int MAX_KEYWORD_TAGS = 10;
        private const int MAX_LINKS_PER_PAGE = 25;

        private static readonly string[] ALLOWABLE_LINK_SCHEMAS = ["http", "https"];

        public PageNormaliser(ILogger logger, ICache blobCache, IEventBus eventBus)
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
            string? title, string? keywords, IEnumerable<string>? tags, IEnumerable<Uri>? links, string? languageIso3)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.ScrapePageResult;

            var normalisedPageResult = new NormalisePageResultDto
            {
                OriginalUrl = result.OriginalUrl,
                Url = result.Url,
                StatusCode = result.StatusCode,
                IsRedirect = result.IsRedirect,
                SourceLastModified = result.SourceLastModified,
                Title = title,
                Keywords = keywords,
                Tags = tags,
                Links = links,
                DetectedLanguageIso3 = languageIso3,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _eventBus.PublishAsync(new GraphPageEvent
            {
                CrawlPageRequest = request,
                NormalisePageResult = normalisedPageResult,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: request.Depth);
        }

        public async Task NormalisePageContentAsync(NormalisePageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.ScrapePageResult;

            if (result.BlobId is null || result.Encoding is null)
            {
                _logger.LogDebug("There was no data to normalise.");
                return;
            }

            var htmlDocument = await GetHtmlDocumentAsync(result.BlobId, result.BlobContainer, result.Encoding);
            if (htmlDocument is null)
            {
                _logger.LogDebug($"Normalisation failed. Blob {result.BlobId} could not be found at {result.BlobContainer}");
                return;
            }

            var htmlParser = new HtmlParser(htmlDocument);
            var extractedTitle = htmlParser.ExtractTitle();
            var extractedContent = htmlParser.ExtractContentAsPlainText();
            var detectedLanguageIso3 = LanguageIdentifier.DetectLanguage(extractedContent);
            var extractedLinks = htmlParser.ExtractLinks();

            var normalisedTitle = NormaliseTitle(extractedTitle);
            var normalisedKeywords = NormaliseKeywords(extractedContent, detectedLanguageIso3);
            var normalisedTags = NormaliseTags(extractedContent, detectedLanguageIso3, MAX_KEYWORD_TAGS);
            var normalisedLinks = NormaliseLinks(
                extractedLinks,
                request.Url,
                request.FollowExternalLinks,
                request.RemoveQueryStrings,
                request.PathFilters);

            await PublishGraphEvent(evt, normalisedTitle, normalisedKeywords, normalisedTags, normalisedLinks, detectedLanguageIso3);

            var linkType = request.FollowExternalLinks ? "external" : "internal";
            _logger.LogDebug($"Publishing GraphPageEvent for {result.Url} with {normalisedLinks.Count()} {linkType} links and {normalisedKeywords.Count()} keywords.");
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

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            text = TextNormaliser.Truncate(text, MAX_TITLE_LENGTH);

            return text;
        }

        public string NormaliseContent(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.CollapseWhitespace(text);

            return text;
        }

        public string NormaliseKeywords(string? text, string languageIso3)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.ToLowerCase(text);
            text = TextNormaliser.RemovePunctuation(text);
            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            if (languageIso3 != null)
                text = StopWordFilter.RemoveStopWords(text, languageIso3);
            text = TextNormaliser.RemoveDuplicateWords(text);
            text = TextNormaliser.TruncateToWords(text, MAX_KEYWORDS);

            return text;
        }

        public IEnumerable<string> NormaliseTags(string? text, string languageIso3, int maxTags)
        {
            if (text == null) return Enumerable.Empty<string>();

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.ToLowerCase(text);
            text = TextNormaliser.RemovePunctuation(text);
            text = TextNormaliser.RemoveSpecialCharacters(text);
            text = TextNormaliser.CollapseWhitespace(text);
            if (languageIso3 != null)
                text = StopWordFilter.RemoveStopWords(text, languageIso3);
            text = TextNormaliser.RemoveNumericalWords(text);

            return TextNormaliser.ExtractTags(text, maxTags);
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

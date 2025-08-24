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
        private const int MAX_SUMMARY_WORDS = 100;
        private const int MAX_KEYWORDS = 300; // one page of text
        private const int MAX_KEYWORD_TAGS = 10;
        private const int MAX_LINKS_PER_PAGE = 100;

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

        private async Task PublishGraphEvent(
            NormalisePageEvent evt,
            string? title, 
            string? summary,
            string? keywords, 
            IEnumerable<string>? tags, 
            IEnumerable<Uri>? links, 
            Uri? imageUrl,
            string? languageIso3)
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
                Summary = summary,
                Keywords = keywords,
                Tags = tags,
                Links = links,
                ImageUrl = imageUrl,
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
            var extractedTitle = htmlParser.ExtractTitle(request.Options.TitleElementXPath);
            var extractedSummary = htmlParser.ExtractContentAsPlainText(request.Options.SummaryElementXPath);
            var extractedContent = htmlParser.ExtractContentAsPlainText(request.Options.ContentElementXPath);
            var detectedLanguageIso3 = LanguageIdentifier.DetectLanguage(extractedContent);
            var extractedLinks = htmlParser.ExtractLinks(request.Options.RelatedLinksElementXPath);
            var extractedImageUrl = htmlParser.ExtractImageUrl(request.Options.ImageElementXPath);   

            var normalisedTitle = NormaliseTitle(extractedTitle);
            var normalisedSummary = NormaliseSummary(extractedSummary);
            var normalisedKeywords = NormaliseKeywords(extractedContent, detectedLanguageIso3);
            var normalisedTags = NormaliseTags(extractedContent, detectedLanguageIso3, MAX_KEYWORD_TAGS);
            var normalisedLinks = NormaliseLinks(
                extractedLinks,
                request.Url,
                request.Options.ExcludeExternalLinks,
                request.Options.ExcludeQueryStrings,
                request.Options.MaxLinks,
                request.Options.UrlMatchRegex);
            var normaliedImageUrl = NormaliseImageUrl(extractedImageUrl, request.Url);

            await PublishGraphEvent(evt, normalisedTitle, normalisedSummary, normalisedKeywords, normalisedTags, normalisedLinks, normaliedImageUrl, detectedLanguageIso3);

            var linkType = request.Options.ExcludeExternalLinks ? "internal" : "external";
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

        public string NormaliseSummary(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.TruncateToWords(text, MAX_SUMMARY_WORDS);

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
            bool excludeExternalLinks,
            bool excludeQueryStrings,
            int maxLinks,
            string linkUrlFilterRegex)
        {
            var uniqueUrls = UrlNormaliser.MakeAbsolute(links, baseUrl);

            uniqueUrls = UrlNormaliser.RemoveCyclicalLinks(uniqueUrls, baseUrl);

            //uniqueUrls = UrlNormaliser.RemoveTrailingSlash(uniqueUrls);

            uniqueUrls = UrlNormaliser.FilterBySchema(uniqueUrls, ALLOWABLE_LINK_SCHEMAS);

            if (excludeExternalLinks)
                uniqueUrls = UrlNormaliser.RemoveExternalLinks(uniqueUrls, baseUrl);

            if (excludeQueryStrings)
                uniqueUrls = UrlNormaliser.RemoveQueryStrings(uniqueUrls);

            uniqueUrls = UrlNormaliser.FilterByRegex(uniqueUrls, linkUrlFilterRegex);

            uniqueUrls = UrlNormaliser.Truncate(uniqueUrls, GetLinkLimit(maxLinks));

            return uniqueUrls;
        }

        public Uri? NormaliseImageUrl(
            string imageUrl,
            Uri baseUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            var uniqueUrls = UrlNormaliser.MakeAbsolute(new List<string> { imageUrl }, baseUrl);

            uniqueUrls = UrlNormaliser.FilterBySchema(uniqueUrls, ALLOWABLE_LINK_SCHEMAS);

            return uniqueUrls.FirstOrDefault();
        }

        private int GetLinkLimit(int maxLinks)
        {
            if (maxLinks <= 0) return 0;
            if (maxLinks > MAX_LINKS_PER_PAGE) return MAX_LINKS_PER_PAGE;
            return maxLinks;
        }
    }
}

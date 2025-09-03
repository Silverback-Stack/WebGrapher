using System;
using System.Text;
using Caching.Core;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;
using Normalisation.Core.Processors;

namespace Normalisation.Core
{
    public class PageNormaliser : IPageNormaliser, IEventBusLifecycle
    {
        private readonly NormalisationSettings _settings;
        private readonly ILogger _logger;
        private readonly ICache _blobCache;
        private readonly IEventBus _eventBus;

        public PageNormaliser(NormalisationSettings settings, ILogger logger, ICache blobCache, IEventBus eventBus)
        {
            _settings = settings;
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

        public async Task PublishClientLogEventAsync(
            Guid graphId,
            Guid? correlationId,
            LogType type,
            string message,
            string? code = null,
            Object? context = null)
        {
            var clientLogEvent = new ClientLogEvent
            {
                GraphId = graphId,
                CorrelationId = correlationId,
                Type = type,
                Message = message,
                Code = code,
                Service = _settings.ServiceName,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
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

            var contentFingerprint = FingerprintHelper.ComputeFingerprint(request.FingerprintCompositeKey);

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
                ContentFingerprint = contentFingerprint,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Check if request is Preview of Normalised data
            LogContextPreview? preview = null;
            if (request.Preview) {
                preview = new LogContextPreview
                {
                    Title = normalisedPageResult.Title,
                    Summary = normalisedPageResult.Summary,
                    Keywords = normalisedPageResult.Keywords,
                    Tags = normalisedPageResult.Tags,
                    Links = normalisedPageResult.Links?.Select(l => l.AbsoluteUri),
                    ImageUrl = normalisedPageResult.ImageUrl?.AbsoluteUri,
                    DetectedLanguageIso3 = normalisedPageResult.DetectedLanguageIso3
                };
            }
            else
            {
                // Not a Preview - continue to Publish GraphPageEvent
                await _eventBus.PublishAsync(new GraphPageEvent
                {
                    CrawlPageRequest = request,
                    NormalisePageResult = normalisedPageResult,
                    CreatedAt = DateTimeOffset.UtcNow
                }, priority: request.Depth);
            }

            var logMessage = $"Normalisation Completed: {result.Url} Links: {links?.Count()} Keywords: {keywords?.Count()}";
            _logger.LogInformation(logMessage);

            await PublishClientLogEventAsync(
                request.GraphId,
                request.CorrelationId,
                LogType.Information,
                logMessage,
                "NormalisationSuccess",
                new LogContext
                {
                    Url = request.Url.AbsoluteUri,
                    TotalLinks = normalisedPageResult.Links?.Count() ?? 0,
                    TotalKeywords = normalisedPageResult.Keywords?.Count() ?? 0,
                    Preview = preview
                });
        }

        public async Task NormalisePageContentAsync(NormalisePageEvent evt)
        {
            string logMessage;
            var request = evt.CrawlPageRequest;
            var result = evt.ScrapePageResult;

            if (result.BlobId is null || result.Encoding is null)
            {
                logMessage = "Normalisation Failed: No data to normalise.";
                _logger.LogError(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    logMessage,
                    "NormalisationFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri
                    });
                return;
            }

            var htmlDocument = await GetHtmlDocumentAsync(result.BlobId, result.BlobContainer, result.Encoding);
            if (htmlDocument is null)
            {
                logMessage = $"Normalisation failed. Blob {result.BlobId} could not be found at {result.BlobContainer}";
                _logger.LogError(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    logMessage,
                    "NormalisationFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri
                    });
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
            var normalisedTags = NormaliseTags(extractedContent, detectedLanguageIso3, _settings.MaxKeywordTags);
            var normalisedLinks = NormaliseLinks(
                extractedLinks,
                request.Url,
                request.Options.ExcludeExternalLinks,
                request.Options.ExcludeQueryStrings,
                request.Options.MaxLinks,
                request.Options.UrlMatchRegex);
            var normaliedImageUrl = NormaliseImageUrl(extractedImageUrl, request.Url);

            await PublishGraphEvent(evt, normalisedTitle, normalisedSummary, normalisedKeywords, normalisedTags, normalisedLinks, normaliedImageUrl, detectedLanguageIso3);
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
            text = TextNormaliser.Truncate(text, _settings.MaxTitleLength);

            return text;
        }

        public string NormaliseSummary(string? text)
        {
            if (text == null) return string.Empty;

            text = TextNormaliser.DecodeHtml(text);
            text = TextNormaliser.TruncateToWords(text, _settings.MaxSummaryWords);

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
            text = TextNormaliser.TruncateToWords(text, _settings.MaxKeywords);

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

            uniqueUrls = UrlNormaliser.FilterBySchema(uniqueUrls, _settings.AllowableLinkSchemas);

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

            uniqueUrls = UrlNormaliser.FilterBySchema(uniqueUrls, _settings.AllowableLinkSchemas);

            return uniqueUrls.FirstOrDefault();
        }

        private int GetLinkLimit(int maxLinks)
        {
            if (maxLinks <= 0) return 0;
            if (maxLinks > _settings.MaxLinksPerPage) return _settings.MaxLinksPerPage;
            return maxLinks;
        }
    }
}

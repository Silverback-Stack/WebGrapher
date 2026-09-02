using Caching.Core;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Microsoft.Extensions.Logging;
using Normalisation.Core.Helpers;
using Normalisation.Core.Processors;
using Requests.Core;
using System;
using System.Text;

namespace Normalisation.Core
{
    public class PageNormaliser : IPageNormaliser, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IRequestSender _requestSender;
        private readonly ICache _blobCache;
        private readonly NormalisationSettings _normalisationSettings;

        public PageNormaliser(
            ILogger logger, 
            IEventBus eventBus, 
            IRequestSender requestSender, 
            ICache blobCache, 
            NormalisationSettings normalisationSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _requestSender = requestSender;
            _blobCache = blobCache;
            _normalisationSettings = normalisationSettings;
        }


        /// <summary>
        /// Starts listening for page normalisation events.
        /// </summary>
        public async Task StartAsync()
        {
            await _eventBus.SubscribeAsync<NormalisePageEvent>(
                _normalisationSettings.ServiceName, NormalisePageAsync);
        }


        /// <summary>
        /// Stops listening for page normalisation events.
        /// </summary>
        public async Task StopAsync()
        {
            await _eventBus.UnsubscribeAsync<NormalisePageEvent>(
                _normalisationSettings.ServiceName, NormalisePageAsync);
        }


        /// <summary>
        /// Normalises a webpage and publishes the outcome.
        /// </summary>
        public async Task NormalisePageAsync(NormalisePageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.ScrapePageResult;

            // Check for cached data reference
            if (result.BlobId is null || result.Encoding is null)
            {
                var logMessage = "Normalisation Failed: No data to normalise.";
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


            // Get cached data using reference
            var htmlPage = await GetCachedHtmlPageAsync(
                result.BlobId,
                result.BlobContainer,
                result.Encoding);


            if (htmlPage is null)
            {
                _logger.LogError("Normalisation failed: Blob {BlobId} could not be found at {BlobContainer}",
                    result.BlobId, result.BlobContainer);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    $"Normalisation failed: Blob {result.BlobId} could not be found at {result.BlobContainer}",
                    "NormalisationFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri
                    });

                return;
            }



            try
            {
                // Extract raw page data
                var rawPageData = ExtractPageData(
                    htmlPage,
                    request);

                // Standardise raw page data
                var standardisedPageData = await StandardisePageDataAsync(
                    rawPageData,
                    request);

                // Filter standardised page data according to request options
                standardisedPageData = FilterPageDataToRequestOptions(
                    standardisedPageData,
                    request);

                // Publish standardised page data
                await PublishGraphEventAsync(
                    evt,
                    standardisedPageData);

            }
            catch (HtmlProcessorException ex)
            {
                // Data extraction exception - include friendly XPath error
                await LogExceptionAsync(
                    ex,
                    request,
                    ex.Message);
            }
            catch (Exception ex) 
            {
                // Unhandled exception
                await LogExceptionAsync(
                    ex,
                    request);
            }
        }

        private async Task LogExceptionAsync(
            Exception ex, 
            CrawlPageRequestDto request, 
            string? xpathError = null)
        {
            if (xpathError != null)
            {
                _logger.LogError(
                    ex, 
                    "Normalisation failed: {PageUrl} XPathError: {xPathError}", 
                    request.Url, 
                    xpathError);
            } else
            {
                _logger.LogError(
                    ex,
                    "Normalisation failed: {PageUrl}", 
                    request.Url);
            }

            // Send friendly message to client
            var clientMessage = xpathError != null
                ? $"Normalisation failed: {xpathError}"
                : $"Normalisation failed: {request.Url}";


            await PublishClientLogEventAsync(
                request.GraphId,
                request.CorrelationId,
                LogType.Error,
                clientMessage,
                "NormalisationFailed",
                new LogContext
                {
                    Url = request.Url.AbsoluteUri
                }
            );
        }


        /// <summary>
        /// Extracts data from a webpage.
        /// </summary>
        private PageDataRaw ExtractPageData(string htmlPage, CrawlPageRequestDto request)
        {
            var rawPageData = new PageDataRaw();

            var htmlProcessor = new HtmlProcessor(htmlPage);

            rawPageData.Title = htmlProcessor.ExtractTitle
                (request.Options.TitleElementXPath);

            rawPageData.Summary = htmlProcessor.ExtractContentAsPlainText(
                request.Options.SummaryElementXPath,
                "Summary Container");

            rawPageData.Content = htmlProcessor.ExtractContentAsPlainText(
                request.Options.ContentElementXPath,
                "Content Container");

            rawPageData.LanguageIso3 = LanguageProcessor.DetectLanguage(
                rawPageData.Content,
                _normalisationSettings.LanguageDetectionFallbackIso3Code);

            rawPageData.LinkReferences = htmlProcessor.ExtractLinkReferences(
                request.Options.RelatedLinksElementXPath);

            rawPageData.ImageReference = htmlProcessor.ExtractImageReference(
                request.Options.ImageElementXPath);

            return rawPageData;
        }


        /// <summary>
        /// Standardises page data.
        /// </summary>
        private async Task<PageDataStandardised> StandardisePageDataAsync(
            PageDataRaw rawPageData, CrawlPageRequestDto request)
        {
            var standardisedPageData = new PageDataStandardised();

            standardisedPageData.Title = StandardiseTitle(
                rawPageData.Title);

            standardisedPageData.Summary = StandardiseSummary(
                rawPageData.Summary);

            standardisedPageData.Keywords = StandardiseTextIntoKeywords(
                rawPageData.Content,
                rawPageData.LanguageIso3);

            standardisedPageData.Tags = StandardiseTextIntoTags(
                rawPageData.Content,
                rawPageData.LanguageIso3,
                _normalisationSettings.MaxTags);

            standardisedPageData.Links = StandardiseLinks(
                rawPageData.LinkReferences,
                request.Url);

            standardisedPageData.ImageUrl = StandardiseImageUrl(
                rawPageData.ImageReference,
                request.Url);

            standardisedPageData.ImageCors = await StandardiseImageCorsAsync(
                standardisedPageData.ImageUrl,
                request);

            standardisedPageData.LanguageIso3 = rawPageData.LanguageIso3;

            return standardisedPageData;
        }


        /// <summary>
        /// Determines image CORS support, defaulting to true when it cannot be determined.
        /// </summary>
        private async Task<bool> StandardiseImageCorsAsync(
            Uri? imageUrl,
            CrawlPageRequestDto request)
        {
            if (imageUrl is null)
                return true;

            var image = await _requestSender.FetchAsync(
                imageUrl,
                request.Options.UserAgent,
                request.Options.UserAccepts);

            return image?.Metadata.HasCorsPolicy ?? true;
        }


        /// <summary>
        /// Filters page data according to crawl page request options.
        /// </summary>
        private PageDataStandardised FilterPageDataToRequestOptions(PageDataStandardised pageData, CrawlPageRequestDto request)
        {
            pageData.Links = FilterLinksToRequestOptions(
                pageData.Links,
                request.Url,
                request.Options.ExcludeExternalLinks,
                request.Options.ExcludeQueryStrings,
                request.Options.MaxLinks,
                request.Options.UrlMatchRegex);

            return pageData;
        }



        /// <summary>
        /// Publishes a log event for the client.
        /// </summary>
        public async Task PublishClientLogEventAsync(
            Guid graphId,
            Guid? correlationId,
            LogType type,
            string message,
            string? code = null,
            Object? context = null //when using a dynamic object type we need to add hints to the strongly typed classes so that .net 9 serialized property names in camelCase
            )
        {
            var clientLogEvent = new ClientLogEvent
            {
                GraphId = graphId,
                CorrelationId = correlationId,
                Type = type,
                Message = message,
                Code = code,
                Service = _normalisationSettings.ServiceName,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
        }


        /// <summary>
        /// Publishes the normalised page data.
        /// </summary>
        private async Task PublishGraphEventAsync(
            NormalisePageEvent evt,
            PageDataStandardised pageData)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.ScrapePageResult;

            // Generate a unique fingerprint representing the page data.
            var fingerprint = FingerprintHelper.ComputeFingerprint(pageData);

            var normalisePageResult = new NormalisePageResultDto
            {
                OriginalUrl = result.OriginalUrl,
                Url = result.Url,
                StatusCode = result.StatusCode,
                IsRedirect = result.IsRedirect,
                SourceLastModified = result.SourceLastModified,
                Title = pageData.Title,
                Summary = pageData.Summary,
                Keywords = pageData.Keywords,
                Tags = pageData.Tags,
                Links = pageData.Links,
                ImageUrl = pageData.ImageUrl,
                ImageCors = pageData.ImageCors,
                DetectedLanguageIso3 = pageData.LanguageIso3,
                Fingerprint = fingerprint,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Check if request is Preview of Normalised data
            LogContextPreview? preview = null;
            if (request.Preview) {
                preview = new LogContextPreview
                {
                    Title = normalisePageResult.Title,
                    Summary = normalisePageResult.Summary,
                    Keywords = normalisePageResult.Keywords,
                    Tags = normalisePageResult.Tags,
                    Links = normalisePageResult.Links?.Select(l => l.AbsoluteUri),
                    ImageUrl = normalisePageResult.ImageUrl?.AbsoluteUri,
                    ImageCors = normalisePageResult.ImageCors,
                    DetectedLanguageIso3 = normalisePageResult.DetectedLanguageIso3
                };
            }
            else
            {
                // Not a Preview - continue to Publish GraphPageEvent
                await _eventBus.PublishAsync(new GraphPageEvent
                {
                    CrawlPageRequest = request,
                    NormalisePageResult = normalisePageResult,
                    CreatedAt = DateTimeOffset.UtcNow
                }, priority: request.Depth);
            }

            _logger.LogInformation("Normalisation Completed: {Url} Links: {LinkCount} Keywords: {KeywordCount}",
                result.Url, pageData.Links?.Count(), pageData.Keywords?.Count());

            await PublishClientLogEventAsync(
                request.GraphId,
                request.CorrelationId,
                LogType.Information,
                $"Normalisation Completed: {result.Url} Links: {pageData.Links?.Count()} Keywords: {pageData.Keywords?.Count()}",
                "NormalisationSuccess",
                new LogContext
                {
                    Url = request.Url.AbsoluteUri,
                    TotalLinks = normalisePageResult.Links?.Count() ?? 0,
                    TotalKeywords = normalisePageResult.Keywords?.Count() ?? 0,
                    Preview = preview
                });
        }




        /// <summary>
        /// Retrieves a cached HTML page.
        /// </summary>
        private async Task<string?> GetCachedHtmlPageAsync(
            string blobId, 
            string? container,
            string encoding)
        {
            byte[]? blob;

            // Get data from cache container
            if (string.IsNullOrWhiteSpace(container) ||
                _blobCache.Container == container)
            {
                blob = await _blobCache.GetAsync<byte[]>(blobId);
            }
            else
            {
                blob = await _blobCache.GetFromContainerAsync<byte[]>(
                    blobId, container);
            }


            if (blob is null)
            {
                _logger.LogWarning(
                    "Blob {BlobId} was not found in cache container {Container}.", 
                    blobId, 
                    container);

                return null;
            }


            // Decode cached data
            try
            {
                var encoder = Encoding.GetEncoding(encoding);
                return encoder.GetString(blob);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, 
                    "Unable to decode cached blob {BlobId}", 
                    blobId);

                return null;
            }
        }


        /// <summary>
        /// Standardises a page title.
        /// </summary>
        public string StandardiseTitle(string? text)
        {
            if (text == null) return string.Empty;

            text = HtmlProcessor.DecodeHtml(text);

            text = TextProcessor.CollapseWhitespace(text);

            text = TextProcessor.LimitTextLength(text, _normalisationSettings.MaxTitleLength);

            return text;
        }


        /// <summary>
        /// Standardise a page summary.
        /// </summary>
        public string StandardiseSummary(string? text)
        {
            if (text == null) return string.Empty;

            text = HtmlProcessor.DecodeHtml(text);

            text = TextProcessor.LimitTextLength(
                text, _normalisationSettings.MaxSummaryLength);

            return text;
        }


        /// <summary>
        /// Standardises text into keywords.
        /// </summary>
        public string StandardiseTextIntoKeywords(
            string? text, string? languageIso3)
        {
            if (text == null) return string.Empty;

            text = HtmlProcessor.DecodeHtml(text);

            if (languageIso3 != null)
            {
                text = StopWordProcessor.RemoveStopWords(
                    text,
                    languageIso3,
                    _normalisationSettings.LanguageDetectionFallbackIso2Code);
            }

            text = TextProcessor.CollapseWhitespace(text);

            text = TextProcessor.RemovePunctuation(text);

            text = TextProcessor.RemoveSpecialCharacters(text);

            text = TextProcessor.RemoveDuplicateWords(text);

            text = TextProcessor.LimitTextLength(
                text, _normalisationSettings.MaxKeywordsLength);

            text = TextProcessor.ToLowerCase(text);

            return text;
        }


        /// <summary>
        /// Standardises text into tags.
        /// </summary>
        public IEnumerable<string> StandardiseTextIntoTags(
            string? text, string? languageIso3, int maxTags)
        {
            if (text == null) return Enumerable.Empty<string>();

            text = HtmlProcessor.DecodeHtml(text);

            if (languageIso3 != null)
            {
                text = StopWordProcessor.RemoveStopWords(
                    text,
                    languageIso3,
                    _normalisationSettings.LanguageDetectionFallbackIso2Code);
            }

            text = TextProcessor.CollapseWhitespace(text);
            
            text = TextProcessor.RemovePunctuation(text);
            
            text = TextProcessor.RemoveSpecialCharacters(text);

            text = TextProcessor.RemoveNumericalWords(text);

            text = TextProcessor.ToLowerCase(text);

            var tags = TextProcessor.ExtractTags(text, maxTags);

            return tags;
        }


        /// <summary>
        /// Standardises link URI references from a webpage into absolute URLs.
        /// </summary>
        public IEnumerable<Uri> StandardiseLinks(
            IEnumerable<string>? linkUriReferences, Uri baseUrl)
        {
            if (linkUriReferences is null) return Enumerable.Empty<Uri>();

            var linkUrls = UrlProcessor.MakeAbsolute(linkUriReferences, baseUrl);

            linkUrls = UrlProcessor.RemoveCyclicalLinks(linkUrls, baseUrl);

            //NEVER REMOVE TRAILING SLASHES - always honour the sites url exactly
            //otherwise can cause unnessesary canonical redirects
            //uniqueUrls = UrlNormaliser.RemoveTrailingSlash(uniqueUrls);

            linkUrls = UrlProcessor.FilterByScheme(linkUrls, _normalisationSettings.AllowedLinkSchemes);

            return linkUrls;
        }


        /// <summary>
        /// Filters links according to request options.
        /// </summary>
        public IEnumerable<Uri> FilterLinksToRequestOptions(
            IEnumerable<Uri>? linkUrls,
            Uri baseUrl,
            bool excludeExternalLinks,
            bool excludeQueryStrings,
            int maxLinks,
            string linkUrlFilterRegex)
        {
            if (linkUrls is null)
                return Enumerable.Empty<Uri>();

            var filteredUrls = linkUrls.ToHashSet();

            var regexPatterns = TextProcessor.SplitLines(
                linkUrlFilterRegex);

            if (excludeExternalLinks)
                filteredUrls = UrlProcessor.RemoveExternalLinks(filteredUrls, baseUrl);

            if (excludeQueryStrings)
                filteredUrls = UrlProcessor.RemoveQueryStrings(filteredUrls);

            filteredUrls = UrlProcessor.FilterByRegex(filteredUrls, regexPatterns);

            filteredUrls = UrlProcessor.LimitLinks(filteredUrls, GetLinkLimit(maxLinks));

            return filteredUrls;
        }


        /// <summary>
        /// Standardises an image reference from a webpage.
        /// </summary>
        public Uri? StandardiseImageUrl(
            string? imageReference,
            Uri baseUrl)
        {
            if (string.IsNullOrWhiteSpace(imageReference))
                return null;

            var imageUrls = UrlProcessor.MakeAbsolute(
                new List<string> { imageReference }, 
                baseUrl);

            imageUrls = UrlProcessor.FilterByScheme(
                imageUrls, 
                _normalisationSettings.AllowedLinkSchemes);

            return imageUrls.FirstOrDefault();
        }


        /// <summary>
        /// Gets the permitted link limit for a webpage.
        /// </summary>
        private int GetLinkLimit(int maxLinks)
        {
            if (maxLinks <= 0) return 0;
            if (maxLinks > _normalisationSettings.MaxLinks) return _normalisationSettings.MaxLinks;
            return maxLinks;
        }
    }
}

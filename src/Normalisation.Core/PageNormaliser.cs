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
                // Extract raw data
                PageDataRaw pageDataRaw = ExtractRawPageData(
                    htmlPage,
                    request);


                // Standardise data
                PageDataStandardised pageDataStandardised = await StandardisePageDataAsync(
                    pageDataRaw,
                    request);


                // Filter data according to request options
                pageDataStandardised = FilterPageDataToRequestOptions(
                    pageDataStandardised,
                    request);


                // Create normalisation result
                NormalisePageResultDto normalisePageResult = CreateNormalisePageResult(
                    result,
                    pageDataStandardised);


                // Publish normalisation result
                await PublishNormalisePageResultAsync(
                    request,
                    normalisePageResult);

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


        /// <summary>
        /// Creates the normalisation result from the scraped and standardised page data.
        /// </summary>
        private NormalisePageResultDto CreateNormalisePageResult(
            ScrapePageResultDto scrapePageResult, 
            PageDataStandardised pageDataStandardised)
        {

            // Generate a unique fingerprint representing the page data.
            var fingerprint = FingerprintHelper.ComputeFingerprint(pageDataStandardised);

            return new NormalisePageResultDto
            {
                CreatedAt = DateTimeOffset.UtcNow,

                // Metadata
                OriginalUrl = scrapePageResult.OriginalUrl,
                Url = scrapePageResult.Url,
                StatusCode = scrapePageResult.StatusCode,
                IsRedirect = scrapePageResult.IsRedirect,
                SourceLastModified = scrapePageResult.SourceLastModified,

                // Normalised data
                Title = pageDataStandardised.Title,
                Summary = pageDataStandardised.Summary,
                Keywords = pageDataStandardised.Keywords,
                Tags = pageDataStandardised.Tags,
                Links = pageDataStandardised.Links,
                ImageUrl = pageDataStandardised.ImageUrl,
                ImageCors = pageDataStandardised.ImageCors,
                DetectedLanguageIso3 = pageDataStandardised.LanguageIso3,
                Fingerprint = fingerprint
            };
        }

        /// <summary>
        /// Logs a normalisation exception with details about the failed request.
        /// </summary>
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
        /// Extracts raw data from a webpage.
        /// </summary>
        private PageDataRaw ExtractRawPageData(
            string htmlPage, 
            CrawlPageRequestDto request)
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
            PageDataRaw pageDataRaw, 
            CrawlPageRequestDto request)
        {
            var standardisedPageData = new PageDataStandardised();

            standardisedPageData.Title = StandardiseTitle(
                pageDataRaw.Title);

            standardisedPageData.Summary = StandardiseSummary(
                pageDataRaw.Summary);

            standardisedPageData.Keywords = StandardiseTextIntoKeywords(
                pageDataRaw.Content,
                pageDataRaw.LanguageIso3);

            standardisedPageData.Tags = StandardiseTextIntoTags(
                pageDataRaw.Content,
                pageDataRaw.LanguageIso3,
                _normalisationSettings.MaxTags);

            standardisedPageData.Links = StandardiseLinks(
                pageDataRaw.LinkReferences,
                request.Url);

            standardisedPageData.ImageUrl = StandardiseImageUrl(
                pageDataRaw.ImageReference,
                request.Url);

            standardisedPageData.ImageCors = await StandardiseImageCorsAsync(
                standardisedPageData.ImageUrl,
                request);

            standardisedPageData.LanguageIso3 = pageDataRaw.LanguageIso3;

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
        private PageDataStandardised FilterPageDataToRequestOptions(
            PageDataStandardised pageDataStandardised, 
            CrawlPageRequestDto request)
        {
            pageDataStandardised.Links = FilterLinksToRequestOptions(
                pageDataStandardised.Links,
                request.Url,
                request.Options.ExcludeExternalLinks,
                request.Options.ExcludeQueryStrings,
                request.Options.MaxLinks,
                request.Options.UrlMatchRegex);

            return pageDataStandardised;
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
        /// Publishes the normalised page result.
        /// </summary>
        private async Task PublishNormalisePageResultAsync(
            CrawlPageRequestDto crawlPageRequest,
            NormalisePageResultDto normalisePageResult)
        {

            // Check if only preview required
            LogContextPreview? logPreview = null;
            if (crawlPageRequest.Preview)
            {
                // Log a Preview of Normalised data
                logPreview = new LogContextPreview
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
                // Publish GraphPageEvent
                await _eventBus.PublishAsync(new GraphPageEvent
                {
                    CrawlPageRequest = crawlPageRequest,
                    NormalisePageResult = normalisePageResult,
                    CreatedAt = DateTimeOffset.UtcNow
                }, priority: crawlPageRequest.Depth);
            }

            _logger.LogInformation("Normalisation Completed: {Url} Links: {LinkCount} Keywords: {KeywordCount}",
                crawlPageRequest.Url, normalisePageResult.Links?.Count(), normalisePageResult.Keywords?.Count());

            await PublishClientLogEventAsync(
                crawlPageRequest.GraphId,
                crawlPageRequest.CorrelationId,
                LogType.Information,
                $"Normalisation Completed: {crawlPageRequest.Url} Links: {normalisePageResult.Links?.Count()} Keywords: {normalisePageResult.Keywords?.Count()}",
                "NormalisationSuccess",
                new LogContext
                {
                    Url = crawlPageRequest.Url.AbsoluteUri,
                    TotalLinks = normalisePageResult.Links?.Count() ?? 0,
                    TotalKeywords = normalisePageResult.Keywords?.Count() ?? 0,
                    Preview = logPreview
                });
        }




        /// <summary>
        /// Retrieves and decodes an HTML page from the cache.
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
        /// Standardise summary text.
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

            text = TextProcessor.RemoveNumericStrings(text);

            text = TextProcessor.ToLowerCase(text);

            var tags = TextProcessor.ExtractTags(text, maxTags);

            return tags;
        }


        /// <summary>
        /// Standardises link references from a webpage into absolute URLs.
        /// </summary>
        public IEnumerable<Uri> StandardiseLinks(
            IEnumerable<string>? linkReferences, Uri baseUrl)
        {
            if (linkReferences is null) return Enumerable.Empty<Uri>();

            var linkUrls = UrlProcessor.MakeAbsolute(linkReferences, baseUrl);

            linkUrls = UrlProcessor.RemoveCyclicalLinks(linkUrls, baseUrl);

            // WARNING: DO NOT REMOVE TRAILING SLASHES
            // always honor the sites url exactly otherwise can cause unnessesary canonical redirects

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
            string urlMatchRegex)
        {
            if (linkUrls is null)
                return Enumerable.Empty<Uri>();

            var filteredUrls = linkUrls.ToHashSet();

            var regexPatterns = TextProcessor.SplitLines(
                urlMatchRegex);

            if (excludeExternalLinks)
                filteredUrls = UrlProcessor.RemoveExternalLinks(filteredUrls, baseUrl);

            if (excludeQueryStrings)
                filteredUrls = UrlProcessor.RemoveQueryStrings(filteredUrls);

            filteredUrls = UrlProcessor.FilterByRegex(filteredUrls, regexPatterns);

            filteredUrls = UrlProcessor.LimitLinks(
                filteredUrls, 
                GetLinkLimit(maxLinks),
                _normalisationSettings.MaxLinksBytes);

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

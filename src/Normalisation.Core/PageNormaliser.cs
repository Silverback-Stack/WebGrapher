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
                // Normalise page data
                var pageData = await NormalisePageDataAsync(htmlPage, request);

                // Filter page data according to request options
                pageData = FilterPageDataToRequestOptions(pageData, request);

                // Publish normalised data
                await PublishGraphEventAsync(
                    evt,
                    pageData);

            }
            catch (NormalisationException ex) // normalisation pipeline exceptions
            {
                // Log full details including inner exception
                // Eg: "Title Container XPath is invalid; check your expression."
                _logger.LogError(ex, "Normalisation failed: {Message}", ex.Message);

                // Send friendly message to client
                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    $"Normalisation failed: {ex.Message}",
                    "NormalisationFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri
                    });

            }
            catch (Exception ex) // unhandled exceptions
            {
                // Log full details
                _logger.LogError(ex, "Normalisation failed: {Url}", request.Url);

                // Send friendly message to client
                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    $"Normalisation failed: {request.Url}",
                    "NormalisationFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri
                    }
                );
            }

        }


        /// <summary>
        /// Extracts and standardises data from a webpage.
        /// </summary>
        private async Task<PageData> NormalisePageDataAsync(string htmlPage, CrawlPageRequestDto request)
        {
            // Extract data from html page

            var htmlProcessor = new HtmlProcessor(htmlPage);
            
            var extractedTitle = htmlProcessor.ExtractTitle
                (request.Options.TitleElementXPath);

            var extractedSummary = htmlProcessor.ExtractContentAsPlainText(
                request.Options.SummaryElementXPath,
                "Summary Container");

            var extractedContent = htmlProcessor.ExtractContentAsPlainText(
                request.Options.ContentElementXPath,
                "Content Container");

            var detectedLanguageIso3 = LanguageProcessor.DetectLanguage(
                extractedContent, 
                _normalisationSettings.LanguageDetectionFallbackIso3Code);

            var extractedLinks = htmlProcessor.ExtractLinks(
                request.Options.RelatedLinksElementXPath);

            var extractedImageUrl = htmlProcessor.ExtractImageUrl(
                request.Options.ImageElementXPath);


            // Standardise extracted data into a PageData object
            var pageData = new PageData();

            pageData.Title = StandardiseTitle(
                extractedTitle);

            pageData.Summary = StandardiseSummary(
                extractedSummary);

            pageData.Keywords = StandardiseContentIntoKeywords(
                extractedContent,
                detectedLanguageIso3);

            pageData.Tags = StandardiseContentIntoTags(
                extractedContent,
                detectedLanguageIso3,
                _normalisationSettings.MaxTags);

            pageData.Links = StandardiseLinks(
                extractedLinks,
                request.Url);

            pageData.ImageUrl = StandardiseImageUrl(
                extractedImageUrl,
                request.Url);

            pageData.ImageCors = await StandardiseImageCorsAsync(
                pageData.ImageUrl,
                request);

            pageData.LanguageIso3 = detectedLanguageIso3;

            return pageData;
        }


        /// <summary>
        /// Determines image CORS support, defaulting to true when it cannot be determined.
        /// </summary>s
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
        private PageData FilterPageDataToRequestOptions(PageData pageData, CrawlPageRequestDto request)
        {
            pageData.Links = FilterLinksToRequestOptions(
                pageData.Links?.ToHashSet(),
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
            PageData pageData)
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
        /// Standardises page content.
        /// </summary>
        public string StandardiseContentIntoText(string? text)
        {
            if (text == null) return string.Empty;

            text = HtmlProcessor.DecodeHtml(text);

            text = TextProcessor.CollapseWhitespace(text);

            return text;
        }


        /// <summary>
        /// Standardises page content into keywords.
        /// </summary>
        public string StandardiseContentIntoKeywords(
            string? text, string? languageIso3)
        {
            if (text == null) return string.Empty;

            text = HtmlProcessor.DecodeHtml(text);

            text = TextProcessor.CollapseWhitespace(text);

            text = TextProcessor.RemovePunctuation(text);

            text = TextProcessor.RemoveSpecialCharacters(text);

            if (languageIso3 != null)
            {
                text = StopWordProcessor.RemoveStopWords(
                    text, 
                    languageIso3, 
                    _normalisationSettings.LanguageDetectionFallbackIso2Code);
            }

            text = TextProcessor.RemoveDuplicateWords(text);

            text = TextProcessor.LimitTextLength(
                text, _normalisationSettings.MaxKeywordsLength);

            text = TextProcessor.ToLowerCase(text);

            return text;
        }


        /// <summary>
        /// Standardises page content into tags.
        /// </summary>
        public IEnumerable<string> StandardiseContentIntoTags(
            string? text, string? languageIso3, int maxTags)
        {
            if (text == null) return Enumerable.Empty<string>();

            text = HtmlProcessor.DecodeHtml(text);

            text = TextProcessor.CollapseWhitespace(text);
            
            text = TextProcessor.RemovePunctuation(text);
            
            text = TextProcessor.RemoveSpecialCharacters(text);

            if (languageIso3 != null)
            {
                text = StopWordProcessor.RemoveStopWords(
                    text, 
                    languageIso3, 
                    _normalisationSettings.LanguageDetectionFallbackIso2Code);
            }

            text = TextProcessor.RemoveNumericalWords(text);

            text = TextProcessor.ToLowerCase(text);

            var tags = TextProcessor.ExtractTags(text, maxTags);

            return tags;
        }


        /// <summary>
        /// Standardises links from a webpage.
        /// </summary>
        public IEnumerable<Uri> StandardiseLinks(
            IEnumerable<string> links, Uri baseUrl)
        {
            var uniqueUrls = UrlProcessor.MakeAbsolute(links, baseUrl);

            uniqueUrls = UrlProcessor.RemoveCyclicalLinks(uniqueUrls, baseUrl);

            //NEVER REMOVE TRAILING SLASHES - always honour the sites url exactly
            //otherwise can cause unnessesary canonical redirects
            //uniqueUrls = UrlNormaliser.RemoveTrailingSlash(uniqueUrls);

            uniqueUrls = UrlProcessor.FilterByScheme(uniqueUrls, _normalisationSettings.AllowedLinkSchemes);

            return uniqueUrls;
        }


        /// <summary>
        /// Filters links according to request options.
        /// </summary>
        public IEnumerable<Uri> FilterLinksToRequestOptions(
            HashSet<Uri>? links,
            Uri baseUrl,
            bool excludeExternalLinks,
            bool excludeQueryStrings,
            int maxLinks,
            string linkUrlFilterRegex)
        {
            if (links is null)
                return Enumerable.Empty<Uri>();

            var filteredLinks = links;

            if (excludeExternalLinks)
                filteredLinks = UrlProcessor.RemoveExternalLinks(filteredLinks, baseUrl);

            if (excludeQueryStrings)
                filteredLinks = UrlProcessor.RemoveQueryStrings(filteredLinks);

            filteredLinks = UrlProcessor.FilterByRegex(filteredLinks, linkUrlFilterRegex);

            filteredLinks = UrlProcessor.LimitLinks(filteredLinks, GetLinkLimit(maxLinks));

            return filteredLinks;
        }


        /// <summary>
        /// Standardises an image URL from a webpage.
        /// </summary>
        public Uri? StandardiseImageUrl(
            string? imageUrl,
            Uri baseUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            var uniqueUrls = UrlProcessor.MakeAbsolute(
                new List<string> { imageUrl }, 
                baseUrl);

            uniqueUrls = UrlProcessor.FilterByScheme(
                uniqueUrls, 
                _normalisationSettings.AllowedLinkSchemes);

            return uniqueUrls.FirstOrDefault();
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

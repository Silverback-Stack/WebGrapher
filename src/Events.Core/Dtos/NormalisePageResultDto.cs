using System.Net;

namespace Events.Core.Dtos
{
    public record NormalisePageResultDto
    {
        public DateTimeOffset CreatedAt { get; init; }

        // Metadata
        public required Uri OriginalUrl { get; init; }
        public required Uri Url { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public bool IsRedirect { get; init; }
        public DateTimeOffset? SourceLastModified { get; init; }

        // Normalised page data
        public string? Title { get; init; }
        public string? Summary { get; init; }
        public string? Keywords { get; init; }
        public IEnumerable<string>? Tags { get; init; }
        public IEnumerable<Uri>? Links { get; init; }
        public Uri? ImageUrl { get; init; }

        /// <summary>
        /// Indicates whether the image can be loaded directly using CORS.
        /// </summary>
        public bool ImageCors { get; init; }

        /// <summary>
        /// The detected ISO 639-3 language code for the page content.
        /// </summary>
        public string? DetectedLanguageIso3 { get; init; }

        /// <summary>
        /// A unique fingerprint representing the normalised page data.
        /// </summary>
        public required string Fingerprint { get; init; }

        /// <summary>
        /// The page URL without query string or fragment components.
        /// </summary>
        public Uri CanonicalUrl => new Uri(
            Url.GetLeftPart(UriPartial.Path));

    }

    
}

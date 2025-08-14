using System.Net;

namespace Events.Core.Dtos
{
    public record NormalisePageResultDto
    {
        //Meta data:
        public required Uri OriginalUrl { get; init; }
        public required Uri Url { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public bool IsRedirect { get; init; }
        public DateTimeOffset? SourceLastModified { get; init; }

        //Normalised data:
        public string? Title { get; init; }
        public string? Summery { get; init; }
        public string? Keywords { get; init; }
        public IEnumerable<string>? Tags { get; init; }
        public IEnumerable<Uri>? Links { get; init; }
        public Uri? ImageUrl { get; init; }
        public string? DetectedLanguageIso3 { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}

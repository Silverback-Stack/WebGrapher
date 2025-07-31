
namespace Streaming.Core.Dtos
{
    public record PageNodeDto
    {
        public string Url { get; init; }
        public string Title { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public List<PageNodeDto> Links { get; init; } = new();
    }
}

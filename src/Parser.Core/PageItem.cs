
namespace Parser.Core
{
    public record PageItem
    {
        public string? Title {  get; init; }
        public string? Content { get; init; }

        public DateTimeOffset LastModified { get; init; }

        public IEnumerable<string> Links { get; init; } = new List<string>();
    }
}

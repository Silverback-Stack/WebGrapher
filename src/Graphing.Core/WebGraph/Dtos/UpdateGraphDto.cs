
namespace Graphing.Core.WebGraph.Dtos
{
    public class UpdateGraphDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }

        public required CrawlRequestDto CrawlRequest { get; init; }
    }
}

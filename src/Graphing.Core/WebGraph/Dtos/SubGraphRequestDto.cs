
namespace Graphing.Core.WebGraph.Dtos
{
    public class SubGraphRequestDto
    {
        public required Uri NodeUrl { get; set; }
        public int MaxDepth { get; set; } = 1;
        public int? MaxNodes { get; set; } = null;
    }
}

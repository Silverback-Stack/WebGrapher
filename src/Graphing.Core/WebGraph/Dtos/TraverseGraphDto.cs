
namespace Graphing.Core.WebGraph.Dtos
{
    public class TraverseGraphDto
    {
        public required Uri StartUrl { get; set; }
        public int MaxDepth { get; set; }
        public int? MaxNodes { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace Graphing.Core.WebGraph.Dtos
{
    public class CrawlPageDto
    {
        public required CrawlRequestDto CrawlRequest { get; init; }

        public bool OverwriteDefaults { get; init; } = false;
        public bool Preview { get; init; } = false;
    }
}


using System.ComponentModel.DataAnnotations;

namespace Graphing.Core.WebGraph.Dtos
{
    public class CrawlPageDto
    {
        [Required]
        public required string Url { get; set; }
        [Range(1, 10)]
        public required int MaxDepth { get; set; }
        [Range(1, 100)]
        public required int MaxLinks { get; set; }
        public bool ExcludeExternalLinks { get; init; } = true;
        public bool ExcludeQueryStrings { get; init; } = true;
        public string UrlMatchRegex { get; init; } = string.Empty;
        public string TitleElementXPath { get; init; } = string.Empty;
        public string ContentElementXPath { get; init; } = string.Empty;
        public string SummaryElementXPath { get; init; } = string.Empty;
        public string ImageElementXPath { get; init; } = string.Empty;
        public string RelatedLinksElementXPath { get; init; } = string.Empty;
    }
}

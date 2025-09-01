using System;

namespace Graphing.Core.WebGraph.Dtos
{
    public class GraphDto
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

        public string Url { get; set; } = string.Empty;
        public int MaxLinks { get; set; } = 10;
        public int MaxDepth { get; set; } = 3;
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

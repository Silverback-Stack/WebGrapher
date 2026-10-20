using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Dtos
{
    public record CrawlRequestDto
    {
        public string? Url { get; init; }

        public int MaxDepth { get; init; } = 1;
        public int MaxLinks { get; init; } = 1;

        public bool ExcludeExternalLinks { get; init; } = true;
        public bool ExcludeQueryStrings { get; init; } = true;
        public bool ConsolidateQueryStrings { get; init; } = true;

        public string UrlMatchRegex { get; init; } = string.Empty;
        public string TitleElementXPath { get; init; } = string.Empty;
        public string ContentElementXPath { get; init; } = string.Empty;
        public string SummaryElementXPath { get; init; } = string.Empty;
        public string ImageElementXPath { get; init; } = string.Empty;
        public string RelatedLinksElementXPath { get; init; } = string.Empty;
    }
}
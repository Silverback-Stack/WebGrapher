using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Dtos
{
    public class UpdateGraphDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
        public required int MaxDepth { get; set; }
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

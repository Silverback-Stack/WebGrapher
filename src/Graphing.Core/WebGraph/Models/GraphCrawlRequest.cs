using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Models
{
    public class GraphCrawlRequest
    {
        public Uri? Url { get; set; }
        public int MaxDepth { get; set; } = 1;
        public int MaxLinks { get; set; } = 1;
        public bool ExcludeExternalLinks { get; set; } = true;
        public bool ExcludeQueryStrings { get; set; } = true;
        public bool ConsolidateQueryStrings { get; set; } = true;
        public string UrlMatchRegex { get; set; } = string.Empty;
        public string TitleElementXPath { get; set; } = string.Empty;
        public string ContentElementXPath { get; set; } = string.Empty;
        public string SummaryElementXPath { get; set; } = string.Empty;
        public string ImageElementXPath { get; set; } = string.Empty;
        public string RelatedLinksElementXPath { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Models
{
    public record Graph
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int MaxDepth { get; init; }
        public int MaxLinks { get; init; }
        public bool FollowExternalLinks { get; init; }
        public bool ExcludeQueryStrings { get; init; }
        public string UrlMatchRegex { get; init; }
        public string TitleElementXPath { get; init; }
        public string ContentElementXPath { get; init; }
        public string SummaryElementXPath { get; init; }
        public string ImageElementXPath { get; init; }
        public string RelatedLinksElementXPath { get; init; }
    }
}

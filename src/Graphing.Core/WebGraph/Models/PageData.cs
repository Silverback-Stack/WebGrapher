using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphing.Core.WebGraph.Models
{
    public class PageData
    {
        public required string Url { get; set; }
        public required string OriginalUrl { get; set; }
        public bool IsRedirect { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }

        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }
        public bool ImageCors { get; set; }
        public string? Keywords { get; set; }
        public IEnumerable<string>? Tags { get; set; }
        public required IEnumerable<string> Links { get; set; }
        public string? DetectedLanguageIso3 { get; set; }
        public required string ContentFingerprint { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normalisation.Core
{
    public class PageDataRaw
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public IEnumerable<string>? Tags { get; set; }
        public IEnumerable<string>? LinkReferences { get; set; }
        public string? ImageReference { get; set; }
        public bool ImageCors { get; set; }
        public string? LanguageIso3 { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normalisation.Core
{
    public class PageDataStandardised
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Keywords { get; set; }
        public IEnumerable<string>? Tags { get; set; }
        public IEnumerable<Uri>? Links { get; set; }
        public Uri? ImageUrl { get; set; }
        public bool ImageCors { get; set; }
        public string? LanguageIso3 { get; set; }
    }
}

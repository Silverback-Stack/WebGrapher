using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserService
{
    public record Page
    {
        public string Title {  get; set; }
        public string Content { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public IEnumerable<string> Links = new List<string>();
    }
}

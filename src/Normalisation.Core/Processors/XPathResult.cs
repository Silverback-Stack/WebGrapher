using HtmlAgilityPack;
using System;

namespace Normalisation.Core.Processors
{
    public class XPathResult
    {
        public XPathResultType Type { get; set; } = XPathResultType.Empty;
        public IEnumerable<HtmlNode>? Nodes { get; set; }
        public string? StringValue { get; set; }
    }
}

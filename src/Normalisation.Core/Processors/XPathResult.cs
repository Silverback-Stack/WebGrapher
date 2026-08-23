using HtmlAgilityPack;
using System;

namespace Normalisation.Core.Processors
{
    /// <summary>
    /// Represents the result of an XPath expression.
    /// </summary>
    public class XPathResult
    {
        public XPathResultType Type { get; set; } = XPathResultType.Empty;
        public IEnumerable<HtmlNode>? Nodes { get; set; }
        public string? StringValue { get; set; }
    }
}

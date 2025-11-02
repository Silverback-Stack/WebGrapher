using System.Net;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace Normalisation.Core.Processors
{
    public class HtmlParser
    {
        private readonly HtmlDocument _htmlDocument;
        private readonly NormalisationSettings _normalisationSettings;

        public HtmlParser(string htmlDocument, NormalisationSettings normalisationSettings) {

            _htmlDocument = new HtmlDocument();
            _htmlDocument.OptionFixNestedTags = true;
            _htmlDocument.LoadHtml(htmlDocument);
            _normalisationSettings = normalisationSettings;
        }

        /// <summary>
        /// Extracts a title from nodes matching the XPath expression, 
        /// or falls back to the document <title> element.
        /// </summary>
        public string ExtractTitle(string xPathExpression = "")
        {
            if (!string.IsNullOrWhiteSpace(xPathExpression))
            {
                var nodes = GetNodes(xPathExpression) ?? Enumerable.Empty<HtmlNode>();
                var result = nodes
                    .Select(n => n.InnerText?.Trim())
                    .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }

            // Fallback to <title>
            var titleNode = _htmlDocument.DocumentNode.SelectSingleNode(_normalisationSettings.Processors.TitleXPath);
            return titleNode?.InnerText.Trim() ?? string.Empty;
        }


        /// <summary>
        /// Extracts plain text content from nodes matching the XPath expression. 
        /// Falls back to the document root if no expression is provided.
        /// Returns an empty string if expression is provided but nothing matches.
        /// </summary>
        public string ExtractContentAsPlainText(string xPathExpression = "")
        {
            var nodes = string.IsNullOrWhiteSpace(xPathExpression)
                ? GetRoot()
                : GetNodes(xPathExpression) ?? Enumerable.Empty<HtmlNode>(); // empty if no match

            var result = string.Join(" ",
                nodes
                    .Select(n => n.InnerText?.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
            ).Trim();

            return result;
        }



        /// <summary>
        /// Extracts links found in nodes matching the XPath expression.
        /// Falls back to entire document if no expression is provided.
        /// </summary>
        public IEnumerable<string> ExtractLinks(string xPathExpression = "")
        {
            // Normalize to IEnumerable<HtmlNode>
            var nodes = string.IsNullOrWhiteSpace(xPathExpression)
                ? GetRoot()
                : GetNodes(xPathExpression) ?? Enumerable.Empty<HtmlNode>(); // empty if no match

            var links = nodes
                .SelectMany(node => node.SelectNodes(_normalisationSettings.Processors.LinksXPath) ?? Enumerable.Empty<HtmlNode>())
                .Select(tag => WebUtility.HtmlDecode(tag.GetAttributeValue("href", "")))
                .Where(href => !string.IsNullOrEmpty(href))
                .ToHashSet();

            return links;
        }


        /// <summary>
        /// Extracts the first valid image URL found in nodes matching the XPath expression.
        /// </summary>
        public string ExtractImageUrl(string xPathExpression = "")
        {
            if (string.IsNullOrEmpty(xPathExpression))
                return string.Empty;

            var nodes = GetNodes(xPathExpression) ?? Enumerable.Empty<HtmlNode>();

            foreach (var node in nodes)
            {
                // Try src
                var src = node.GetAttributeValue("src", string.Empty);
                if (!string.IsNullOrEmpty(src))
                    return src;

                // Try srcset — get first URL
                var srcset = node.GetAttributeValue("srcset", string.Empty);
                if (!string.IsNullOrEmpty(srcset))
                {
                    var firstUrl = srcset.Split(',')[0].Trim().Split(' ')[0];
                    if (!string.IsNullOrEmpty(firstUrl))
                        return firstUrl;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the document root node.
        /// </summary>
        private IEnumerable<HtmlNode> GetRoot()
        {
            return new[] { _htmlDocument.DocumentNode };
        }

        /// <summary>
        /// Picks filtered nodes or null.
        /// </summary>
        private IEnumerable<HtmlNode>? GetNodes(string xPathExpression)
        {
            if (IsValidExpression(xPathExpression))
            {
                var nodes = _htmlDocument.DocumentNode.SelectNodes(xPathExpression);
                if (nodes != null && nodes.Any())
                    return nodes;
            }

            return null;
        }

        private bool IsValidExpression(string xPathExpression)
        {
            if (string.IsNullOrWhiteSpace(xPathExpression))
                return false;

            try
            {
                // Validate XPath syntax
                XPathExpression.Compile(xPathExpression);
            }
            catch (Exception)
            {
                //invalid Xpath syntax
                return false;
            }

            return true;
        }
    }
}

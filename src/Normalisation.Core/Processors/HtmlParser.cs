using System.Net;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace Normalisation.Core.Processors
{
    public class HtmlParser
    {
        private const string TITLE_XPATH = ".//title";
        private const string LINKS_XPATH = ".//a[@href]";

        private readonly HtmlDocument _htmlDocument;

        public HtmlParser(string htmlDocument) {

            _htmlDocument = new HtmlDocument();
            _htmlDocument.OptionFixNestedTags = true;
            _htmlDocument.LoadHtml(htmlDocument);
        }

        public string ExtractTitle(string xPathExpression = "")
        {
            var node = GetNodeOrRoot(xPathExpression);
            var result = node.InnerText?.Trim() ?? string.Empty;

            // If empty, try extracting from page <title>
            if (string.IsNullOrWhiteSpace(result) || node == _htmlDocument.DocumentNode)
            {
                var titleNode = _htmlDocument.DocumentNode.SelectSingleNode(TITLE_XPATH);
                if (!string.IsNullOrWhiteSpace(titleNode?.InnerText))
                    result = titleNode.InnerText.Trim();
            }

            return result;
        }

        public string ExtractSummaryAsPlainText(string xPathExpression = "")
        {
            if (string.IsNullOrWhiteSpace(xPathExpression))
                return string.Empty;

            var node = GetNode(xPathExpression);

            if (node == null)
                return string.Empty;

            return node.InnerText?.Trim() ?? string.Empty;
        }

        public string ExtractContentAsPlainText(string xPathExpression = "")
        {
            var node = GetNodeOrRoot(xPathExpression);
            var result = node.InnerText?.Trim() ?? string.Empty;

            // Fallback: entire document text if empty
            if (string.IsNullOrWhiteSpace(result))
                result = _htmlDocument.DocumentNode.InnerText?.Trim() ?? string.Empty;

            return result;
        }


        public IEnumerable<string> ExtractLinks(string xPathExpression = "")
        {
            HtmlNode node;

            if (!string.IsNullOrEmpty(xPathExpression))
            {
                node = GetNode(xPathExpression);
                if (node == null)
                    return Enumerable.Empty<string>(); // filter provided but container not found
            }
            else
            {
                node = _htmlDocument.DocumentNode; // no filter → whole document
            }

            var links = (node.SelectNodes(LINKS_XPATH) ?? Enumerable.Empty<HtmlNode>())
                .Select(tag => WebUtility.HtmlDecode(tag.GetAttributeValue("href", "")))
                .Where(href => !string.IsNullOrEmpty(href))
                .ToHashSet();

            return links;
        }

        public string ExtractImageUrl(string xPathExpression = "")
        {
            if (string.IsNullOrEmpty(xPathExpression))
                return string.Empty;

            var node = GetNode(xPathExpression);
            if (node == null)
                return string.Empty;

            // Try src
            var src = node.GetAttributeValue("src", null);
            if (!string.IsNullOrEmpty(src))
                return src;

            // Try srcset — get first URL
            var srcset = node.GetAttributeValue("srcset", null);
            if (!string.IsNullOrEmpty(srcset))
            {
                var firstUrl = srcset.Split(',')[0].Trim().Split(' ')[0];
                return firstUrl;
            }

            return string.Empty;
        }

        /// <summary>
        /// Picks either a filtered node or the document root, and guarantees non-null.
        /// </summary>
        private HtmlNode GetNodeOrRoot(string xPathExpression)
        {
            if (IsValidExpression(xPathExpression))
            {
                var node = _htmlDocument.DocumentNode.SelectSingleNode(xPathExpression);
                if (node != null)
                    return node;
            }

            // Default to entire document if no valid node found
            return _htmlDocument.DocumentNode;
        }

        /// <summary>
        /// Picks a filtered node or null.
        /// </summary>
        private HtmlNode? GetNode(string xPathExpression)
        {
            if (IsValidExpression(xPathExpression))
            {
                var node = _htmlDocument.DocumentNode.SelectSingleNode(xPathExpression);
                return node;
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

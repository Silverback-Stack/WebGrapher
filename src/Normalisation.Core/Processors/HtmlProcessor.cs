using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace Normalisation.Core.Processors
{
    public class HtmlProcessor
    {
        private readonly HtmlDocument _htmlDocument;
        private readonly XPathProcessor _xPathProcessor;

        private const string DefaultTitleXPath = ".//title"; // <title> elements
        private const string DefaultLinksXPath = ".//a[@href]"; // <a> elements with an href attribute

        private const string HttpUrlPattern = @"https?://\S+"; // Matches HTTP or HTTPS URLs

        public HtmlProcessor(string htmlPage) {

            _htmlDocument = new HtmlDocument();
            _htmlDocument.OptionFixNestedTags = true;
            _htmlDocument.LoadHtml(htmlPage);
            _xPathProcessor = new XPathProcessor(_htmlDocument);
        }


        /// <summary>
        /// Decodes HTML entities into their corresponding characters.
        /// </summary>
        public static string DecodeHtml(string text)
        {
            return WebUtility.HtmlDecode(text);
        }



        /// <summary>
        /// Extracts a title from elements matching the XPath expression, 
        /// or falls back to the document <title> element.
        /// </summary>
        public string ExtractTitle(string xPathExpression = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(xPathExpression))
                {
                    var xPathResult = _xPathProcessor.Evaluate(xPathExpression);

                    switch (xPathResult.Type)
                    {
                        case XPathResultType.NodeSet:
                            // Return the first non-empty matching node.
                            return xPathResult.Nodes?
                                .Select(n => n.InnerText?.Trim())
                                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                                ?? string.Empty;

                        case XPathResultType.String:
                            return xPathResult.StringValue 
                                ?? string.Empty;
                    }
                }

                // Fallback to <title>
                var titleNode = _htmlDocument.DocumentNode
                    .SelectSingleNode(DefaultTitleXPath);
                
                return titleNode?.InnerText.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Friendly message for client, include original exception internally
                throw new NormalisationException("Title Container XPath is invalid; check your expression.", ex);
            }
        }


        /// <summary>
        /// Extracts plain text content from elements matching the XPath expression. 
        /// Falls back to the document root if no expression is provided.
        /// Returns an empty string if expression is provided but nothing matches.
        /// </summary>
        public string ExtractContentAsPlainText(string xPathExpression = "", string containerName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xPathExpression))
                    return _htmlDocument.DocumentNode.InnerText.Trim();

                var xPathResult = _xPathProcessor.Evaluate(xPathExpression);

                switch (xPathResult.Type)
                {
                    case XPathResultType.NodeSet:
                        if (xPathResult.Nodes == null || !xPathResult.Nodes.Any())
                            return string.Empty;

                        return string.Join(" ",
                            xPathResult.Nodes
                                .Select(n => n.InnerText.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t)));

                    case XPathResultType.String:
                        return xPathResult.StringValue?.Trim() ?? string.Empty;

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Friendly message for client, include original exception internally
                throw new NormalisationException($"{containerName} XPath is invalid; check your expression.", ex);
            }

        }


        /// <summary>
        /// Extracts links found in elements matching the XPath expression.
        /// Falls back to entire document if no expression is provided.
        /// </summary>
        public IEnumerable<string> ExtractLinks(string xPathExpression = "")
        {
            try
            {
                var xPathResult = string.IsNullOrWhiteSpace(xPathExpression)
                    ? new XPathResult { Type = XPathResultType.NodeSet, Nodes = new[] { _htmlDocument.DocumentNode } }
                    : _xPathProcessor.Evaluate(xPathExpression);

                if (xPathResult.Type != XPathResultType.NodeSet || xPathResult.Nodes == null)
                    return Enumerable.Empty<string>();

                return xPathResult.Nodes
                    .SelectMany(node => node.SelectNodes(DefaultLinksXPath)
                        ?? Enumerable.Empty<HtmlNode>())
                    .Select(tag => DecodeHtml(
                        tag.GetAttributeValue("href", string.Empty)))
                    .Where(href => !string.IsNullOrEmpty(href))
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                throw new NormalisationException("Related Links Container XPath is invalid; check your expression.", ex);
            }
        }


        /// <summary>
        /// Extracts the preferred image URL from elements matching the XPath expression.
        /// </summary>
        public string ExtractImageUrl(string xPathExpression = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xPathExpression))
                    return string.Empty;

                var xPathResult = _xPathProcessor.Evaluate(xPathExpression);

                if (xPathResult.Type != XPathResultType.NodeSet || xPathResult.Nodes == null)
                    return string.Empty;

                foreach (var node in xPathResult.Nodes)
                {
                    // Prefer srcset; the last URL is typically the highest resolution.
                    var srcset = node.GetAttributeValue("srcset", string.Empty);
                    if (!string.IsNullOrEmpty(srcset))
                    {
                        // Example srcset string:
                        // "https://example.com/image1.jpg 190w, https://example.com/image2.jpg 285w, https://example.com/image3.jpg 380w"

                        // Find all matches
                        var urls = Regex.Matches(srcset, HttpUrlPattern)
                                        .Cast<Match>()
                                        .Select(m => m.Value)
                                        .ToArray();

                        // Return the last URL, which is typically the largest/highest-resolution image
                        var largestImageUrl = urls.LastOrDefault();
                        if (!string.IsNullOrEmpty(largestImageUrl))
                            return largestImageUrl;
                    }

                    // Fallback to src
                    var src = node.GetAttributeValue("src", string.Empty);
                    if (!string.IsNullOrEmpty(src)) 
                        return src;

                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new NormalisationException("Image Container XPath is invalid; check your expression.", ex);
            }
        }

    }
}

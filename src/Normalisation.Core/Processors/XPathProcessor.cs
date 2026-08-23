using HtmlAgilityPack;
using System.Xml.XPath;

namespace Normalisation.Core.Processors
{

    /// <summary>
    /// Evaluates XPath expressions against an HTML document.
    /// Supports expressions that return HTML nodes or scalar values and returns the result as a consistent XPathResult.
    /// Invalid or unsupported expressions return an empty result so the caller can decide how to proceed.
    /// </summary>
    public class XPathProcessor
    {
        private readonly HtmlDocument _htmlDocument;

        public XPathProcessor(HtmlDocument htmlDocument)
        {
            _htmlDocument = htmlDocument;
        }


        /// <summary>
        /// Evaluates an XPath expression and returns a consistent XPathResult.
        /// </summary>
        public XPathResult Evaluate(string expression)
        {
            var result = new XPathResult();

            if (string.IsNullOrWhiteSpace(expression))
                return result; // Nothing to evaluate, return empty result


            try {
                // Validate XPath syntax
                XPathExpression.Compile(expression); 
            }
            catch {
                // Invalid XPath cannot be evaluated.
                // Return an empty result so the caller can decide how to proceed.
                return result;
            }

            // Try using HtmlAgilityPack's SelectNodes()
            // This works only for node-set XPath expressions
            IEnumerable<HtmlNode>? nodes = null;
            try
            {
                nodes = _htmlDocument.DocumentNode.SelectNodes(expression);
            }
            catch
            {
                nodes = null; // Failed - Likely scalar expression
            }

            // If nodes were found, return them as a NodeSet result
            if (nodes != null && nodes.Any())
            {
                result.Type = XPathResultType.NodeSet;
                result.Nodes = nodes;
                return result;
            }

            // Fallback: Use XPathNavigator.Evaluate()
            // Handles scalar XPath expressions or any expression SelectNodes cannot handle
            var nav = _htmlDocument.CreateNavigator();
            object eval = nav.Evaluate(expression);

            switch (eval)
            {
                case string s:
                    // Expression returned a string value directly
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        result.Type = XPathResultType.String;
                        result.StringValue = s.Trim();
                    }
                    break;

                case double d:
                    result.Type = XPathResultType.String;
                    result.StringValue = d.ToString();
                    break;

                case bool b:
                    result.Type = XPathResultType.String;
                    result.StringValue = b.ToString();
                    break;

                case XPathNodeIterator iter:
                    // If the XPath returned nodes, take their InnerText
                    var firstNav = iter.Cast<XPathNavigator>()
                       .Select(navItem =>
                       {
                           var doc = new HtmlDocument();
                           doc.LoadHtml(navItem.OuterXml);
                           return doc.DocumentNode.FirstChild;
                       })
                       .FirstOrDefault();

                    if (firstNav != null)
                    {
                        result.Type = XPathResultType.NodeSet;
                        result.Nodes = new[] { firstNav };
                    }
                    break;
            }

            // Return evaluated result, either node-set or string
            return result;
        }
    }
}

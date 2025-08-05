using System;
using System.Net;
using HtmlAgilityPack;

namespace Normalisation.Core.Processors
{
    public class HtmlParser
    {
        private const string TITLE_XPATH = "//head/title";
        private const string LINKS_XPATH = "//a[@href]";

        private readonly HtmlDocument _htmlDocument;

        public HtmlParser(string htmlDocument) {

            _htmlDocument = new HtmlDocument();
            _htmlDocument.LoadHtml(htmlDocument);
        }

        public string ExtractTitle()
        {
            var title = _htmlDocument.DocumentNode.SelectSingleNode(TITLE_XPATH);
            return title != null ? title.InnerText : string.Empty;
        }

        public string ExtractContentAsPlainText()
        {
            return _htmlDocument.DocumentNode.InnerText;
        }

        public IEnumerable<string> ExtractLinks()
        {
            var links = new List<string>();

            //select all <a> tags with href
            var tags = _htmlDocument.DocumentNode.SelectNodes(LINKS_XPATH);
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    var href = WebUtility.HtmlDecode(tag.GetAttributeValue("href", ""));
                    if (!string.IsNullOrEmpty(href)) links.Add(href);
                }
            }
            return links;
        }
    }
}

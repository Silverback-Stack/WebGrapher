using System;
using System.Net;
using Events.Core.Bus;
using HtmlAgilityPack;
using Logging.Core;

namespace ParserService
{
    public class HtmlAgilityPackPageParser : BasePageParser
    {
        private const string TITLE_XPATH = "//head/title";
        private const string LINKS_XPATH = "//a[@href]";


        public HtmlAgilityPackPageParser(ILogger logger, IEventBus eventBus) : base(logger, eventBus) { }

        public override PageItem Parse(string content)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            return new PageItem
            {
                Title = ExtractTitle(htmlDocument),
                Content = ExtractContentAsPlainText(htmlDocument),
                Links = ExtractLinks(htmlDocument)
            };
        }

        private string ExtractTitle(HtmlDocument htmlDocument)
        {
            var title = htmlDocument.DocumentNode.SelectSingleNode(TITLE_XPATH);
            return title != null ? title.InnerText : string.Empty;
        }

        private string ExtractContentAsPlainText(HtmlDocument htmlDocument)
        {
            return htmlDocument.DocumentNode.InnerText;
        }

        private IEnumerable<string> ExtractLinks(HtmlDocument htmlDocument)
        {
            var links = new List<string>();

            //select all <a> tags with href
            var tags = htmlDocument.DocumentNode.SelectNodes(LINKS_XPATH);
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

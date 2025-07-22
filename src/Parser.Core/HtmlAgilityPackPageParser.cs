using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using HtmlAgilityPack;
using Logging.Core;

namespace ParserService
{
    public class HtmlAgilityPackPageParser : BasePageParser
    {
        public HtmlAgilityPackPageParser(ILogger logger, IEventBus eventBus) : base(logger, eventBus) { }

        public override Page Parse(string content)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            return new Page
            {
                Title = ExtractTitle(htmlDocument),
                Content = ExtractContentAsPlainText(htmlDocument),
                Links = ExtractLinks(htmlDocument)
            };
        }

        private string ExtractTitle(HtmlDocument htmlDocument)
        {
            var title = htmlDocument.DocumentNode.SelectSingleNode("//head/title");
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
            var tags = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    var href = tags[i].GetAttributeValue("href", "");
                    href = WebUtility.HtmlDecode(href);

                    if (href != string.Empty)
                    {
                        links.Add(href);
                    }
                }
            }
            return links;
        }
    }
}

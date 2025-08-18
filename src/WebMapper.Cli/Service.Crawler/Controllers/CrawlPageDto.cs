using System.ComponentModel;

namespace WebMapper.Cli.Service.Crawler.Controllers
{
    public class CrawlPageDto
    {
        [DefaultValue("https://www.theaudiodb.com/chart_artists")]
        public required Uri Url { get; set; }

        [DefaultValue(1)]
        public Guid GraphId { get; set; }

        [DefaultValue(false)]
        public bool FollowExternalLinks { get; set; }

        [DefaultValue(true)]
        public bool ExcludeQueryStrings { get; set; }

        [DefaultValue(10)]
        public int MaxLinks { get; set; }

        [DefaultValue(3)]
        public int MaxDepth { get; set; }

        [DefaultValue("^https?://[^/]+/(artist|album)/")]
        public string UrlMatchRegex { get; set; }

        [DefaultValue("//div[@class='col-sm-4']//h1/text()")]
        public string TitleElementXPath { get; set; }

        [DefaultValue("//div[@class='container']/*")]
        public string ContentElementXPath { get; set; }

        [DefaultValue("//div[@class='container']/*")]
        public string SummaryElementXPath { get; set; }

        [DefaultValue("(//div[contains(@class, 'col-sm-4')]//img[1]/@src)[1]")]
        public string ImageElementXPath { get; set; }

        [DefaultValue("")]
        public string RelatedLinksElementXPath { get; set; }


        [DefaultValue("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36")]
        public string UserAgent { get; set; }

        [DefaultValue("text/html")]
        public string UserAccepts { get; set; }
    }
}

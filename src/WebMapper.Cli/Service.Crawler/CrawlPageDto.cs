using System.ComponentModel;

namespace WebMapper.Cli.Service.Crawler
{
    public class CrawlPageDto
    {
        [DefaultValue("https://www.theaudiodb.com/chart_artists")]
        public Uri Url { get; set; }

        [DefaultValue(1)]
        public int MapId { get; set; }

        [DefaultValue(false)]
        public bool FollowExternalLinks { get; set; }

        [DefaultValue(true)]
        public bool RemoveQueryStrings { get; set; }

        [DefaultValue(3)]
        public int MaxDepth { get; set; }

        [DefaultValue("/artist/")]
        public string? PathFilters { get; set; }

        [DefaultValue("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36")]
        public string? UserAgent { get; set; }

        [DefaultValue("text/html")]
        public string? UserAccepts { get; set; }
    }
}

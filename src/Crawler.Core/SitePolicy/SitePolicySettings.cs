
namespace Crawler.Core.SitePolicy
{
    public class SitePolicySettings
    {
        public string UserAccepts { get; set; } = "text/plain, text/html";
        public int AbsoluteExpiryMinutes { get; set; } = 20;
    }
}

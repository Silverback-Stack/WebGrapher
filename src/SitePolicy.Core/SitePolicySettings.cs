
namespace SitePolicy.Core
{
    public class SitePolicySettings
    {
        /// <summary>
        /// Accept header used when retrieving robots.txt files.
        /// </summary>
        public string RobotsUserAccepts { get; set; } = "text/plain, text/html";

        /// <summary>
        /// Default policy lifetime before Site Policies expire.
        /// </summary>
        public int PolicyExpiryMinutes { get; set; } = 20;
    }
}

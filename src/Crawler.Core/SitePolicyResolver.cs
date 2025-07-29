using System;
using System.Net;
using System.Text;
using Logging.Core;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core
{
    public class SitePolicyResolver : ISitePolicyResolver
    {
        private readonly ILogger _logger;
        private readonly IRequestSender _requestSender;

        private const string ROBOTS_TXT_USER_ACCEPTS_OVERRIDE = "text/html,text/plain";

        public SitePolicyResolver(ILogger logger, IRequestSender requestSender)
        {
            _logger = logger;
            _requestSender = requestSender;
        }

        public bool IsRateLimited(SitePolicyItem policy)
        {
            return policy.IsRateLimited;
        }

        public SitePolicyItem MergePolicies(SitePolicyItem existingPolicy, SitePolicyItem newPolicy)
        {
            return existingPolicy.MergePolicy(newPolicy);
        }

        public bool IsPermittedByRobotsTxt(Uri url, string? userAgent, SitePolicyItem policy)
        {
            if (string.IsNullOrWhiteSpace(policy.RobotsTxtContent)) 
                return true;

            if (string.IsNullOrWhiteSpace(userAgent))
            {
                throw new ArgumentException("UserAgent is required to check against RobotsTxt.");
            }

            var robots = new RobotsTxt();
            robots.Load(policy.RobotsTxtContent);

            var isAllowed = robots.IsAllowed(userAgent, url.AbsolutePath);

            return isAllowed;
        }

        public async Task<string?> GetRobotsTxtContentAsync(Uri url, string? userAgent, string? userAccepts)
        {
            var robotsTxtUrl = new Uri($"{url.Scheme}://{url.Host}/robots.txt");

            var response = await _requestSender.GetStringAsync(
                robotsTxtUrl,
                userAgent,
                ROBOTS_TXT_USER_ACCEPTS_OVERRIDE);

            return response?.Data?.Content;
        }

    }
}

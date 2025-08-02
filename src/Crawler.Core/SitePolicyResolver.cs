using System;
using Requests.Core;
using Toimik.RobotsProtocol;

namespace Crawler.Core
{
    public class SitePolicyResolver : ISitePolicyResolver
    {
        private readonly IRequestSender _requestSender;

        private const string ROBOTS_TXT_USER_AGENT_OVERRIDE = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        private const string ROBOTS_TXT_USER_ACCEPTS_OVERRIDE = "text/html,text/plain";

        public SitePolicyResolver(IRequestSender requestSender)
        {
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
                userAgent = ROBOTS_TXT_USER_AGENT_OVERRIDE;
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

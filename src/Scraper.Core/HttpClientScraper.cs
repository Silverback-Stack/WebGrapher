using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;

namespace ScraperService
{
    public class HttpClientScraper : BaseScraper
    {
        private const int CLIENT_REQUESTS_DELAY_SECONDS = 3;

        public HttpClientScraper(IAppLogger appLogger, IEventBus eventBus) : base(appLogger, eventBus) { }

        public override async Task<ScrapeResponse> GetAsync(Uri url, string userAgent, string clientAccept)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                httpClient.DefaultRequestHeaders.Accept.ParseAdd(clientAccept);

                ScrapeResponse? scrapeResponse = null;
                try
                {
                    ////TODO: REMOVE THIS ONCE EVENT MANAGER QUEUE IS IMPLEMENTED
                    ////etiquette to simulate between requests for rate limiters
                    //Thread.Sleep(CLIENT_REQUESTS_DELAY_SECONDS * 1000);

                    //var response = await httpClient.GetAsync(url.AbsoluteUri);
                    //response.EnsureSuccessStatusCode();

                    //var contentType = response.Content.Headers.ContentType?.MediaType;
                    //var lastModified = response.Content?.Headers?.LastModified?.Date ?? DateTimeOffset.UtcNow;

                    //if (contentType != null && ALLOWED_CONTENT_TYPES.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                    //{
                    //    responseDto = new ResponseDto
                    //    {
                    //        StatusCode = response.StatusCode,
                    //        LastModified = lastModified,
                    //        HtmlContent = await response.Content.ReadAsStringAsync()
                    //    };

                    //    _logger.LogInformation($"GET: {url.AbsoluteUri} Status Code: {response.StatusCode}");
                    //}
                }
                catch (Exception ex)
                {
                    _appLogger.LogError($"Error fetching {url.AbsoluteUri}. Exception: {ex.Message} InnerException: {ex.InnerException}");
                }
                return scrapeResponse;
            }
        }

    }

}

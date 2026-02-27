using System;
using Crawler.Core;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Microsoft.Extensions.Hosting;
using WebGrapher.Cli.Services;

namespace WebGrapher.Cli
{
    public class WebGrapherApp
    {
        private IEventBus? _eventBus;
        private IPageCrawler? _pageCrawler;
        private readonly IHostEnvironment _environment;

        public WebGrapherApp(IHostEnvironment environment) {
            _environment = environment;
        }

        public async Task InitializeAsync()
        {
            /*
             * This CLI hosts multiple microservice instances (Crawler, Scraper, Normalisation,
             * Graphing, Streaming) within a single process for local development.
             *
             * Each service is initialized concurrently and runs on its own execution context,
             * emulating separate microservice hosts while sharing an in-memory event bus.
             *
             * All services complete startup and event subscription before the event bus is
             * started, ensuring deterministic initialization order and isolation semantics
             * similar to a distributed environment.
            */


            //Create Event Bus
            _eventBus = await EventBusService.InitializeAsync(_environment);

            // Kick off all service initializations concurrently
            var crawlerTask = CrawlerService.InitializeAsync(_eventBus, _environment);

            var scraperTask = ScraperService.InitializeAsync(_eventBus, _environment);

            var normalisationTask = NormalisationService.InitializeAsync(_eventBus, _environment);

            var graphingTask = GraphingService.InitializeAsync(_eventBus, _environment);

            var streamingTask = StreamingService.InitializeAsync(_eventBus, _environment);

            // Wait for all services to finish subscribing events
            await Task.WhenAll(
                crawlerTask, scraperTask, normalisationTask, graphingTask, streamingTask);

            _pageCrawler = await crawlerTask;

            // Start event bus after all services are ready
            await _eventBus.StartAsync();

            await RunAsync();

            //cleanup
            await GraphingService.StopWebApiServerAsync();
            await StreamingService.StopHubServerAsync();
        }

        private async Task RunAsync()
        {
            while (true)
            {
                // read input
                Console.WriteLine("Enter URL to crawl (or type 'exit' to quit):");
                var input = GetInput();

                // check exit
                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                // validate url
                var url = ValidateUrl(input);
                if (url == null)
                {
                    Console.WriteLine("Invalid URL. Use format: http[s]://www.example.com");
                    continue;
                }

                // submit url
                await SubmitUrlAsync(url);
            }
        }


        /// <summary>
        /// Reads the input from the console.
        /// </summary>
        /// <returns></returns>
        private string? GetInput()
        {
            Console.WriteLine();
            return Console.ReadLine()?.Trim();
        }

        private Uri? ValidateUrl(string? input) =>
            Uri.TryCreate(input, UriKind.Absolute, out var result) ? result : null;


        /// <summary>
        /// Submits a Url for processing.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task SubmitUrlAsync(Uri url)
        {
            if (_pageCrawler is null) return;

            //create a crawl page request
            var crawlPageRequest = new CrawlPageRequestDto
            {
                Url = url,
                GraphId = Guid.Empty, // Empty Guids are assigned to the default graph
                CorrelationId = Guid.NewGuid(),
                Attempt = 1,
                Depth = 0,
                Options = new CrawlPageRequestOptionsDto
                {
                    MaxDepth = 1,
                    MaxLinks = 5,
                    ExcludeExternalLinks = true,
                    ExcludeQueryStrings = true,
                    ConsolidateQueryStrings = true,
                    UrlMatchRegex = "",
                    TitleElementXPath = "",
                    ContentElementXPath = "",
                    SummaryElementXPath = "",
                    ImageElementXPath = "",
                    RelatedLinksElementXPath = "",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
                    UserAccepts = "text/html,text/plain"
                },
                RequestedAt = DateTimeOffset.UtcNow
            };

            //create a crawl page event
            var crawlPageEvent = new CrawlPageEvent
            {
                CrawlPageRequest = crawlPageRequest,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _pageCrawler.EvaluatePageForCrawling(crawlPageEvent);
        }


    }
}

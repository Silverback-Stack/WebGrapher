using System;
using Crawler.Core;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Microsoft.Extensions.Configuration;
using WebGrapher.Cli.Service.Crawler;
using WebGrapher.Cli.Service.Events;
using WebGrapher.Cli.Service.Graphing;
using WebGrapher.Cli.Service.Normalisation;
using WebGrapher.Cli.Service.Scraper;
using WebGrapher.Cli.Service.Streaming;

namespace WebGrapher.Cli
{
    public class WebGrapherApp
    {
        private IEventBus? _eventBus;
        private IPageCrawler? _pageCrawler;

        public WebGrapherApp() { }

        public async Task InitializeAsync()
        {
            //Setup Configuration using appsettings overrides
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Events/appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            //bind appsettings overrides to default settings objects
            var eventBusSettings = new EventBusSettings();
            configuration.GetSection("EventBus").Bind(eventBusSettings);

            _eventBus = await EventBusService.StartAsync(eventBusSettings);

            var crawlerTask = Task.Run(async () 
                => _pageCrawler = await CrawlerService.InitializeAsync(_eventBus));

            var scraperTask = Task.Run(async () 
                => ScraperService.InitializeAsync(_eventBus));

            var normalisationTask = Task.Run(async () 
                => NormalisationService.InitializeAsync(_eventBus));

            var graphingTask = Task.Run(async () 
                => GraphingService.InitializeAsync(_eventBus));

            var streamingTask = Task.Run(async () 
                => StreamingService.InitializeAsync(_eventBus));

            await Task.WhenAll(crawlerTask, scraperTask, normalisationTask, graphingTask, streamingTask);

            await RunAsync();
        }

        private async Task RunAsync()
        {
            var exit = false;
            while (!exit)
            {
                Console.WriteLine("Enter URL to crawl (or type 'exit' to quit):");

                var input = GetInput();

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)) {
                    exit = true;
                    continue;
                }

                var url = GetUri(input);
                if (url != null)
                {
                    await SubmitUrlAsync(url);
                } 
                else
                {
                    Console.WriteLine("Invalid Url. Use format: https://www.example.com");
                }               
            }

            //cleanup
            await GraphingService.StopWebApiServerAsync();
            await StreamingService.StopHubServerAsync();
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

        private Uri? GetUri(string? input) =>
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
                GraphId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CorrelationId = Guid.NewGuid(),
                Attempt = 1,
                Depth = 0,
                Options = new CrawlPageRequestOptionsDto
                {
                    MaxDepth = 1,
                    MaxLinks = 10,
                    ExcludeExternalLinks = true,
                    ExcludeQueryStrings = true,
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

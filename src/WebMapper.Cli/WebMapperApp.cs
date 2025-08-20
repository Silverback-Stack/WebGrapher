using System;
using Crawler.Core;
using Events.Core.Bus;
using Events.Core.Events;
using WebMapper.Cli.Service.Crawler;
using WebMapper.Cli.Service.Events;
using WebMapper.Cli.Service.Graphing;
using WebMapper.Cli.Service.Normalisation;
using WebMapper.Cli.Service.Scraper;
using WebMapper.Cli.Service.Streaming;

namespace WebMapper.Cli
{
    public class WebMapperApp
    {
        private IEventBus? _eventBus;
        private IPageCrawler? _pageCrawler;

        public WebMapperApp() { }

        public async Task InitializeAsync()
        {
            _eventBus = await EventBusService.StartAsync();

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
            await CrawlerService.StopWebApiServerAsync();
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

            await _pageCrawler.EvaluatePageForCrawling(new CrawlPageEvent(
                url: url, 
                graphId: Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                excludeExternalLinks: false,
                excludeQueryStrings: true,
                maxDepth: 3,
                maxLinks: 10,
                urlMatchRegex: "",
                titleElementXPath: "",
                contentElementXPath: "",
                summaryElementXPath: "",
                imageElementXPath: "",
                relatedLinksElementXPath: "",
                userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36",
                userAccepts: "text/html,text/plain"));
        }


    }
}

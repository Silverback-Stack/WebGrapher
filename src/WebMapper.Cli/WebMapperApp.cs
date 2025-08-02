using System;
using Crawler.Core;
using Events.Core.Bus;
using Events.Core.EventTypes;
using WebMapper.Cli.Service.Crawler;
using WebMapper.Cli.Service.Events;
using WebMapper.Cli.Service.Graphing;
using WebMapper.Cli.Service.Normalisation;
using WebMapper.Cli.Service.Parser;
using WebMapper.Cli.Service.Scraper;
using WebMapper.Cli.Service.Streaming;

namespace WebMapper.Cli
{
    internal class WebMapperApp
    {
        private readonly IEventBus _eventBus;
        private IPageCrawler? _pageCrawler;

        public WebMapperApp() {

            _eventBus = EventBusService.Start();

            Task.Run(async () => _pageCrawler = await CrawlerService.InitializeAsync(_eventBus));

            Task.Run(async () => await ScraperService.InitializeAsync(_eventBus));

            Task.Run(async () => await ParserService.InitializeAsync(_eventBus));

            Task.Run(async () => await NormalisationService.InitializeAsync(_eventBus));

            Task.Run(async () => await GraphingService.InitializeAsync(_eventBus));

            Task.Run(async () => await StreamingService.InitializeAsync(_eventBus));
        }


        public async Task Run()
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
                    await SubmitUrl(url);
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
        private async Task SubmitUrl(Uri url)
        {
            if (_pageCrawler is null) return;

            await _pageCrawler.EvaluatePageForCrawling(new CrawlPageEvent(
                url: url, 
                mapId: 1, 
                correlationId: new Guid(),
                followExternalLinks: false,
                removeQueryStrings: true,
                maxDepth: 3,
                pathFilters: null, 
                userAgent: null,
                userAccepts: null));
        }


    }
}

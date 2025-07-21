using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core;
using Events.Core.Bus;
using Events.Core.Types;
using Graphing.Core;
using Normalisation.Core;
using ParserService;
using ScraperService;

namespace WebMapper.Cli
{
    internal class App
    {
        private readonly IEventBus _eventBus;

        public App() {

            _eventBus = EventBusFactory.Create();

            //CRAWLER SERVICE
            Task.Run(async () => {
                var service = CrawlerFactory.Create(CrawlerOptions.Memory, _eventBus);
                await service.StartAsync();
            });

            //SCRAPER SERVICE
            Task.Run(async () => {
                var service = ScraperFactory.Create(_eventBus);
                await service.StartAsync();
            });

            //PARSER SERVICE
            Task.Run(async () => {
                var service = ParserFactory.Create(_eventBus);
                await service.StartAsync();
            });

            //NORMALISATION SERVICE
            Task.Run(async () => {
                var service = NormalisationFactory.Create(_eventBus);
                await service.StartAsync();
            });

            //GRAPHING SERVICE
            Task.Run(async () => {
                var service = GraphingFactory.Create(_eventBus);
                await service.StartAsync();
            });

            //STREAMING SERVICE
            //Task.Run(() => StreamingFactory.Create(_eventBus));
        }

        public async Task Start()
        {
            while (true)
            {
                //READ INPUT:
                var url = GetUrl();

                //SUBMIT INPUT:
                if (url != null)
                    await SubmitUrl(url);
            }
        }

        /// <summary>
        /// Reads the Url input from the console.
        /// </summary>
        /// <returns></returns>
        private Uri? GetUrl()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Url:");
            var url = Console.ReadLine();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Console.WriteLine("Invalid Url. Use format: https://www.example.com");
                return null;
            }

            return uri;
        }

        /// <summary>
        /// Submits a Url for processing.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task SubmitUrl(Uri url)
        {
            var crawlPageEvent = new CrawlPageEvent(url, containerId: Guid.NewGuid(), correlationId: Guid.NewGuid());

            crawlPageEvent.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
            crawlPageEvent.ClientAccepts = "text/html";

            //crawlPageEvent.PathFilters = new string[] { "/movie/", "/tv/", "/person/" }; //https://www.themoviedb.org/movie/
            //crawlPageEvent.PathFilters = new string[] { "/artist/", "/album/", "/track/" }; //https://www.theaudiodb.com/chart_artists
            crawlPageEvent.PathFilters = new string[] { "/title/", "/name/" }; //https://www.imdb.com/
            crawlPageEvent.FollowExternalLinks = false;
            crawlPageEvent.RemoveQueryStrings = true;
            crawlPageEvent.MaxDepth = 5;

            await _eventBus.PublishAsync(crawlPageEvent);
            Console.WriteLine($"{url.AbsoluteUri} was submitted.");
        }


    }
}
